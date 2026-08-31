using EduTrack.Web.Data;
using EduTrack.Web.Models;
using EduTrack.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web.Services;

/// <summary>
/// Service responsible for calculating student GPA/Attendance risk thresholds,
/// flagging at-risk students, and providing system-wide summary analytics.
/// </summary>
public class AtRiskEvaluationService(ApplicationDbContext db, IConfiguration config)
{
    // Thresholds configured in appsettings.json or fallback defaults
    public decimal GpaHighRiskThreshold => decimal.TryParse(config["AtRiskSettings:GpaHighRiskThreshold"], out var v) ? v : 2.25m;
    public decimal GpaMediumRiskThreshold => decimal.TryParse(config["AtRiskSettings:GpaMediumRiskThreshold"], out var v) ? v : 2.75m;
    public decimal AttendanceHighRiskThreshold => decimal.TryParse(config["AtRiskSettings:AttendanceHighRiskThreshold"], out var v) ? v : 60.0m;
    public decimal AttendanceMediumRiskThreshold => decimal.TryParse(config["AtRiskSettings:AttendanceMediumRiskThreshold"], out var v) ? v : 75.0m;

    /// <summary>
    /// Evaluates academic and attendance performance for a single student.
    /// </summary>
    public async Task<StudentAtRiskItem> EvaluateStudentRiskAsync(int studentId)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
        if (student == null) throw new InvalidOperationException($"Student with ID {studentId} not found.");

        var enrollments = await db.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Grade)
            .Where(e => e.StudentId == studentId)
            .ToListAsync();

        return CalculateRiskItem(student, enrollments);
    }

    /// <summary>
    /// Generates filterable At-Risk Warning Panel data for Admin or Teacher view.
    /// </summary>
    public async Task<AtRiskWarningPanelViewModel> GetAtRiskWarningPanelAsync(
        string? department = null,
        string? riskLevel = null,
        string? riskType = null,
        string? search = null,
        int? teacherId = null)
    {
        var studentQuery = db.Students.Where(s => s.IsActive);

        // Filter by teacher if teacherId provided
        if (teacherId.HasValue)
        {
            var teacherStudentIds = await db.Enrollments
                .Include(e => e.Course)
                .Where(e => e.Course != null && e.Course.TeacherId == teacherId.Value)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();

            studentQuery = studentQuery.Where(s => teacherStudentIds.Contains(s.Id));
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            studentQuery = studentQuery.Where(s => s.Department == department);
        }

        var students = await studentQuery.ToListAsync();

        var studentIds = students.Select(s => s.Id).ToList();

        var enrollments = await db.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Grade)
            .Where(e => studentIds.Contains(e.StudentId))
            .ToListAsync();

        var enrollmentsByStudent = enrollments.GroupBy(e => e.StudentId).ToDictionary(g => g.Key, g => g.ToList());

        var allRiskItems = new List<StudentAtRiskItem>();

        foreach (var student in students)
        {
            var studentEnrollments = enrollmentsByStudent.GetValueOrDefault(student.Id, new List<Enrollment>());
            var item = CalculateRiskItem(student, studentEnrollments);
            allRiskItems.Add(item);
        }

        // Available departments for dropdown filter
        var availableDepartments = await db.Students.Select(s => s.Department).Distinct().OrderBy(d => d).ToListAsync();

        // Calculate summary metrics across all evaluated students before applying risk filters
        var totalEvaluated = allRiskItems.Count;
        var highCount = allRiskItems.Count(i => i.RiskLevel == "High");
        var medCount = allRiskItems.Count(i => i.RiskLevel == "Medium");
        var normalCount = allRiskItems.Count(i => i.RiskLevel == "Normal");

        var acadCount = allRiskItems.Count(i => i.RiskType == "Academic");
        var attCount = allRiskItems.Count(i => i.RiskType == "Attendance");
        var bothCount = allRiskItems.Count(i => i.RiskType == "Both");

        var atRiskItems = allRiskItems.Where(i => i.RiskLevel != "Normal").ToList();
        var avgAtRiskGpa = atRiskItems.Any() ? Math.Round(atRiskItems.Average(i => i.RunningGpa), 2) : 0m;
        var avgAtRiskAtt = atRiskItems.Any() ? Math.Round(atRiskItems.Average(i => i.AttendancePercent), 1) : 0m;

        // Apply filters for panel table
        var filtered = allRiskItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(riskLevel) && riskLevel != "All")
        {
            if (riskLevel == "AtRisk")
                filtered = filtered.Where(i => i.RiskLevel != "Normal");
            else
                filtered = filtered.Where(i => i.RiskLevel.Equals(riskLevel, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(riskType) && riskType != "All")
        {
            filtered = filtered.Where(i => i.RiskType.Equals(riskType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            filtered = filtered.Where(i => i.FullName.ToLower().Contains(s) || i.RollNumber.ToLower().Contains(s) || i.Email.ToLower().Contains(s));
        }

        string? teacherName = null;
        if (teacherId.HasValue)
        {
            var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId.Value);
            teacherName = teacher?.FullName;
        }

        return new AtRiskWarningPanelViewModel
        {
            TotalStudentsEvaluated = totalEvaluated,
            TotalAtRiskCount = highCount + medCount,
            HighRiskCount = highCount,
            MediumRiskCount = medCount,
            NormalCount = normalCount,
            AcademicRiskCount = acadCount,
            AttendanceRiskCount = attCount,
            BothRiskCount = bothCount,
            AverageAtRiskGpa = avgAtRiskGpa,
            AverageAtRiskAttendance = avgAtRiskAtt,
            AtRiskStudents = filtered.OrderByDescending(i => i.RiskLevel == "High").ThenByDescending(i => i.RiskLevel == "Medium").ThenBy(i => i.RollNumber).ToList(),
            SelectedDepartment = department,
            SelectedRiskLevel = riskLevel,
            SelectedRiskType = riskType,
            SearchTerm = search,
            AvailableDepartments = availableDepartments,
            IsTeacherScope = teacherId.HasValue,
            TeacherName = teacherName
        };
    }

    /// <summary>
    /// Generates initial system-wide summary report data from live SQL Server queries.
    /// </summary>
    public async Task<SystemReportViewModel> GetSystemReportAsync(string? department = null, string? semester = null)
    {
        var totalStudents = await db.Students.CountAsync();
        var totalTeachers = await db.Teachers.CountAsync();
        var totalCourses = await db.Courses.CountAsync();
        var totalEnrollments = await db.Enrollments.CountAsync();
        var totalGrades = await db.Grades.CountAsync();

        var students = await db.Students.Where(s => s.IsActive).ToListAsync();
        var enrollments = await db.Enrollments.Include(e => e.Course).Include(e => e.Grade).ToListAsync();

        var enrollmentsByStudent = enrollments.GroupBy(e => e.StudentId).ToDictionary(g => g.Key, g => g.ToList());

        var evaluated = new List<StudentAtRiskItem>();
        foreach (var s in students)
        {
            var sEnrollments = enrollmentsByStudent.GetValueOrDefault(s.Id, new List<Enrollment>());
            evaluated.Add(CalculateRiskItem(s, sEnrollments));
        }

        // Apply filters if passed
        var filteredEvaluated = evaluated.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(department))
        {
            filteredEvaluated = filteredEvaluated.Where(e => e.Department == department);
        }

        var evalList = filteredEvaluated.ToList();

        var highRisk = evalList.Count(e => e.RiskLevel == "High");
        var medRisk = evalList.Count(e => e.RiskLevel == "Medium");
        var normal = evalList.Count(e => e.RiskLevel == "Normal");

        var acadRisk = evalList.Count(e => e.RiskType == "Academic");
        var attRisk = evalList.Count(e => e.RiskType == "Attendance");
        var bothRisk = evalList.Count(e => e.RiskType == "Both");

        var avgGpa = evalList.Any(e => e.GradedCoursesCount > 0)
            ? Math.Round(evalList.Where(e => e.GradedCoursesCount > 0).Average(e => e.RunningGpa), 2)
            : 0m;

        var avgAtt = evalList.Any(e => e.GradedCoursesCount > 0)
            ? Math.Round(evalList.Where(e => e.GradedCoursesCount > 0).Average(e => e.AttendancePercent), 1)
            : 0m;

        var belowGpaCount = evalList.Count(e => e.RunningGpa > 0 && e.RunningGpa < GpaMediumRiskThreshold);
        var belowAttCount = evalList.Count(e => e.AttendancePercent > 0 && e.AttendancePercent < AttendanceMediumRiskThreshold);

        // Department Breakdown
        var deptGroups = evaluated.GroupBy(e => e.Department);
        var deptSummaries = new List<DepartmentSummaryItem>();

        foreach (var dg in deptGroups)
        {
            var dStudents = dg.ToList();
            var dGraded = dStudents.Where(s => s.GradedCoursesCount > 0).ToList();

            deptSummaries.Add(new DepartmentSummaryItem
            {
                DepartmentName = dg.Key,
                TotalStudents = dStudents.Count,
                AtRiskCount = dStudents.Count(s => s.RiskLevel != "Normal"),
                HighRiskCount = dStudents.Count(s => s.RiskLevel == "High"),
                MediumRiskCount = dStudents.Count(s => s.RiskLevel == "Medium"),
                AverageGpa = dGraded.Any() ? Math.Round(dGraded.Average(s => s.RunningGpa), 2) : 0m,
                AverageAttendance = dGraded.Any() ? Math.Round(dGraded.Average(s => s.AttendancePercent), 1) : 0m
            });
        }

        // Course Breakdown
        var courses = await db.Courses.Include(c => c.Teacher).ToListAsync();
        var courseSummaries = new List<CourseSummaryReportItem>();

        var enrollmentsByCourse = enrollments.GroupBy(e => e.CourseId).ToDictionary(g => g.Key, g => g.ToList());
        var atRiskStudentIds = evaluated.Where(e => e.RiskLevel != "Normal").Select(e => e.StudentId).ToHashSet();

        foreach (var c in courses)
        {
            var cEnrollments = enrollmentsByCourse.GetValueOrDefault(c.Id, new List<Enrollment>());
            var cGrades = cEnrollments.Where(e => e.Grade != null).Select(e => e.Grade!).ToList();

            var avgMark = cGrades.Any() ? Math.Round(cGrades.Average(g => g.TotalMark), 1) : 0m;
            var passCount = cGrades.Count(g => g.GradePoint > 0m);
            var failCount = cGrades.Count(g => g.GradePoint == 0m);
            var atRiskEnrolled = cEnrollments.Count(e => atRiskStudentIds.Contains(e.StudentId));

            courseSummaries.Add(new CourseSummaryReportItem
            {
                CourseId = c.Id,
                CourseCode = c.CourseCode,
                CourseTitle = c.CourseName,
                TeacherName = c.Teacher?.FullName ?? "Unassigned",
                CreditHours = c.CreditHours,
                TotalEnrolled = cEnrollments.Count,
                GradedCount = cGrades.Count,
                AverageTotalMark = avgMark,
                PassCount = passCount,
                FailCount = failCount,
                AtRiskEnrolledCount = atRiskEnrolled
            });
        }

        var depts = await db.Students.Select(s => s.Department).Distinct().OrderBy(d => d).ToListAsync();
        var semes = await db.Enrollments.Where(e => e.Semester != null).Select(e => e.Semester!).Distinct().OrderBy(s => s).ToListAsync();

        return new SystemReportViewModel
        {
            TotalStudentsCount = totalStudents,
            TotalTeachersCount = totalTeachers,
            TotalCoursesCount = totalCourses,
            TotalEnrollmentsCount = totalEnrollments,
            TotalGradesCount = totalGrades,
            TotalAtRiskCount = highRisk + medRisk,
            HighRiskCount = highRisk,
            MediumRiskCount = medRisk,
            NormalCount = normal,
            AcademicRiskCount = acadRisk,
            AttendanceRiskCount = attRisk,
            BothRiskCount = bothRisk,
            SystemAverageGpa = avgGpa,
            SystemAverageAttendance = avgAtt,
            StudentsBelowGpaThresholdCount = belowGpaCount,
            StudentsBelowAttendanceThresholdCount = belowAttCount,
            DepartmentSummaries = deptSummaries.OrderBy(d => d.DepartmentName).ToList(),
            CourseSummaries = courseSummaries.OrderBy(c => c.CourseCode).ToList(),
            SelectedDepartment = department,
            SelectedSemester = semester,
            AvailableDepartments = depts,
            AvailableSemesters = semes
        };
    }

    /// <summary>
    /// Synchronizes and records current risk evaluations into the AtRiskFlags database table.
    /// </summary>
    public async Task SyncAtRiskFlagsAsync()
    {
        var students = await db.Students.Where(s => s.IsActive).ToListAsync();
        var enrollments = await db.Enrollments.Include(e => e.Course).Include(e => e.Grade).ToListAsync();
        var enrollmentsByStudent = enrollments.GroupBy(e => e.StudentId).ToDictionary(g => g.Key, g => g.ToList());

        var existingFlags = await db.AtRiskFlags.ToListAsync();

        foreach (var student in students)
        {
            var sEnrollments = enrollmentsByStudent.GetValueOrDefault(student.Id, new List<Enrollment>());
            var eval = CalculateRiskItem(student, sEnrollments);

            var flag = existingFlags.FirstOrDefault(f => f.StudentId == student.Id);
            if (flag == null)
            {
                flag = new AtRiskFlag
                {
                    StudentId = student.Id,
                    RunningGpa = eval.RunningGpa,
                    AttendancePercent = eval.AttendancePercent,
                    RiskLevel = eval.RiskLevel,
                    RiskType = eval.RiskType,
                    Reason = eval.Reason,
                    Semester = student.Semester,
                    EvaluatedAt = DateTime.UtcNow
                };
                db.AtRiskFlags.Add(flag);
            }
            else
            {
                flag.RunningGpa = eval.RunningGpa;
                flag.AttendancePercent = eval.AttendancePercent;
                flag.RiskLevel = eval.RiskLevel;
                flag.RiskType = eval.RiskType;
                flag.Reason = eval.Reason;
                flag.Semester = student.Semester;
                flag.EvaluatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }

    private StudentAtRiskItem CalculateRiskItem(Student student, List<Enrollment> studentEnrollments)
    {
        decimal totalPoints = 0m;
        decimal totalCredits = 0m;
        var gradedEnrollments = new List<Enrollment>();

        foreach (var e in studentEnrollments)
        {
            if (e.Grade != null && e.Course != null)
            {
                gradedEnrollments.Add(e);
                totalPoints += e.Grade.GradePoint * e.Course.CreditHours;
                totalCredits += e.Course.CreditHours;
            }
        }

        decimal runningGpa = totalCredits > 0m ? Math.Round(totalPoints / totalCredits, 2) : 0m;

        // Attendance % = Average (AttendanceMark / 10 * 100) across graded enrollments
        decimal attendancePercent = 100m;
        if (gradedEnrollments.Any())
        {
            var attSum = gradedEnrollments.Sum(e => (e.Grade!.AttendanceMark / 10.0m) * 100m);
            attendancePercent = Math.Round(attSum / gradedEnrollments.Count, 1);
        }

        // Determine risk categories based on thresholds
        bool hasGrades = gradedEnrollments.Count > 0;

        bool isGpaHigh = hasGrades && runningGpa < GpaHighRiskThreshold;
        bool isGpaMed = hasGrades && runningGpa >= GpaHighRiskThreshold && runningGpa < GpaMediumRiskThreshold;

        bool isAttHigh = hasGrades && attendancePercent < AttendanceHighRiskThreshold;
        bool isAttMed = hasGrades && attendancePercent >= AttendanceHighRiskThreshold && attendancePercent < AttendanceMediumRiskThreshold;

        string riskLevel = "Normal";
        if (isGpaHigh || isAttHigh) riskLevel = "High";
        else if (isGpaMed || isAttMed) riskLevel = "Medium";

        string riskType = "None";
        if ((isGpaHigh || isGpaMed) && (isAttHigh || isAttMed)) riskType = "Both";
        else if (isGpaHigh || isGpaMed) riskType = "Academic";
        else if (isAttHigh || isAttMed) riskType = "Attendance";

        // Reason formulation
        var reasons = new List<string>();
        if (isGpaHigh) reasons.Add($"Critical GPA ({runningGpa:F2} < {GpaHighRiskThreshold:F2})");
        else if (isGpaMed) reasons.Add($"Low GPA ({runningGpa:F2} < {GpaMediumRiskThreshold:F2})");

        if (isAttHigh) reasons.Add($"Critical Attendance ({attendancePercent:F1}% < {AttendanceHighRiskThreshold:F1}%)");
        else if (isAttMed) reasons.Add($"Low Attendance ({attendancePercent:F1}% < {AttendanceMediumRiskThreshold:F1}%)");

        if (!hasGrades) reasons.Add("No grades recorded yet");
        else if (!reasons.Any()) reasons.Add("Satisfactory academic standing");

        return new StudentAtRiskItem
        {
            StudentId = student.Id,
            RollNumber = student.RollNumber,
            FullName = student.FullName,
            Email = student.Email,
            Department = student.Department,
            Semester = student.Semester,
            RunningGpa = runningGpa,
            AttendancePercent = attendancePercent,
            RiskLevel = riskLevel,
            RiskType = riskType,
            Reason = string.Join(" & ", reasons),
            EnrolledCoursesCount = studentEnrollments.Count,
            GradedCoursesCount = gradedEnrollments.Count,
            EnrolledCourseCodes = studentEnrollments.Where(e => e.Course != null).Select(e => e.Course!.CourseCode).ToList()
        };
    }
}
