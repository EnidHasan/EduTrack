using EduTrack.Web.Data;
using EduTrack.Web.Models;
using EduTrack.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web.Services;

public class AcademicAnalyticsService(ApplicationDbContext db)
{
    public const decimal PassThreshold = 40.0m;

    /// <summary>
    /// Computes full Academic Analytics metrics using SQL Server aggregation.
    /// </summary>
    public async Task<AcademicAnalyticsViewModel> GetAnalyticsAsync(
        AcademicAnalyticsFilter? filter = null,
        int? teacherId = null)
    {
        filter ??= new AcademicAnalyticsFilter();

        // 1. Resolve Teacher info if teacher-scoped
        string? teacherName = null;
        if (teacherId.HasValue)
        {
            var teacher = await db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == teacherId.Value);
            teacherName = teacher?.FullName;
        }

        // 2. Base Course Query for accessible courses
        var courseQuery = db.Courses.AsNoTracking().Where(c => c.IsActive);
        if (teacherId.HasValue)
        {
            courseQuery = courseQuery.Where(c => c.TeacherId == teacherId.Value);
        }

        var accessibleCourses = await courseQuery
            .OrderBy(c => c.CourseCode)
            .Select(c => new CourseLookupItem
            {
                Id = c.Id,
                Code = c.CourseCode,
                Title = c.CourseName
            })
            .ToListAsync();

        var accessibleCourseIds = accessibleCourses.Select(c => c.Id).ToHashSet();

        // If teacher specified a courseId, ensure it belongs to accessible courses
        int? effectiveCourseId = filter.CourseId;
        if (effectiveCourseId.HasValue && !accessibleCourseIds.Contains(effectiveCourseId.Value))
        {
            // If ID is not in teacher's assigned courses, do not leak or query another course
            effectiveCourseId = -1; // Won't match any enrollment
        }

        // 3. Base Enrollment Query
        var enrollmentQuery = db.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Include(e => e.Grade)
            .Where(e => e.IsActive && e.Course != null && accessibleCourseIds.Contains(e.CourseId));

        if (effectiveCourseId.HasValue)
        {
            enrollmentQuery = enrollmentQuery.Where(e => e.CourseId == effectiveCourseId.Value);
        }

        if (filter.AcademicYear.HasValue)
        {
            enrollmentQuery = enrollmentQuery.Where(e => e.AcademicYear == filter.AcademicYear.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Semester) && filter.Semester != "All")
        {
            enrollmentQuery = enrollmentQuery.Where(e => e.Semester == filter.Semester || e.Course!.Semester == filter.Semester);
        }

        // Department filter (only allowed/applied for non-teacher or if student matches)
        if (!string.IsNullOrWhiteSpace(filter.Department) && filter.Department != "All")
        {
            enrollmentQuery = enrollmentQuery.Where(e => e.Student != null && e.Student.Department == filter.Department);
        }

        // 4. Graded Enrollments Query (only graded students are included in calculations)
        var gradedEnrollmentQuery = enrollmentQuery.Where(e => e.Grade != null);

        // Overall KPIs via database queries
        var totalGraded = await gradedEnrollmentQuery.CountAsync();
        var passCount = await gradedEnrollmentQuery.CountAsync(e => e.Grade!.TotalMark >= PassThreshold);
        var failCount = await gradedEnrollmentQuery.CountAsync(e => e.Grade!.TotalMark < PassThreshold);

        decimal averageMark = 0m;
        if (totalGraded > 0)
        {
            var rawAvg = await gradedEnrollmentQuery.AverageAsync(e => (double)e.Grade!.TotalMark);
            averageMark = Math.Round((decimal)rawAvg, 1, MidpointRounding.AwayFromZero);
        }

        decimal passRate = totalGraded > 0
            ? Math.Round((decimal)passCount * 100m / totalGraded, 1, MidpointRounding.AwayFromZero)
            : 0m;

        decimal failRate = totalGraded > 0
            ? Math.Round((decimal)failCount * 100m / totalGraded, 1, MidpointRounding.AwayFromZero)
            : 0m;

        // 5. Course Averages & Performance Breakdown
        // Filter target courses
        var targetCourseIds = accessibleCourseIds.ToList();
        if (effectiveCourseId.HasValue)
        {
            targetCourseIds = targetCourseIds.Where(id => id == effectiveCourseId.Value).ToList();
        }

        var coursesWithTeachers = await db.Courses
            .AsNoTracking()
            .Include(c => c.Teacher)
            .Where(c => targetCourseIds.Contains(c.Id))
            .OrderBy(c => c.CourseCode)
            .ToListAsync();

        // Group enrollments in the filtered set by course
        var enrollmentsByCourse = await enrollmentQuery
            .GroupBy(e => e.CourseId)
            .Select(g => new
            {
                CourseId = g.Key,
                TotalEnrolled = g.Count(),
                GradedCount = g.Count(e => e.Grade != null),
                AverageMark = g.Where(e => e.Grade != null).Select(e => (double?)e.Grade!.TotalMark).Average(),
                PassCount = g.Count(e => e.Grade != null && e.Grade.TotalMark >= PassThreshold),
                FailCount = g.Count(e => e.Grade != null && e.Grade.TotalMark < PassThreshold)
            })
            .ToDictionaryAsync(g => g.CourseId);

        var courseAverages = new List<CourseAverageViewModel>();
        foreach (var c in coursesWithTeachers)
        {
            if (enrollmentsByCourse.TryGetValue(c.Id, out var stat))
            {
                decimal courseAvg = stat.GradedCount > 0 && stat.AverageMark.HasValue
                    ? Math.Round((decimal)stat.AverageMark.Value, 1, MidpointRounding.AwayFromZero)
                    : 0m;

                decimal coursePassRate = stat.GradedCount > 0
                    ? Math.Round((decimal)stat.PassCount * 100m / stat.GradedCount, 1, MidpointRounding.AwayFromZero)
                    : 0m;

                courseAverages.Add(new CourseAverageViewModel
                {
                    CourseId = c.Id,
                    CourseCode = c.CourseCode,
                    CourseTitle = c.CourseName,
                    TeacherName = c.Teacher?.FullName ?? "Unassigned",
                    CreditHours = c.CreditHours,
                    TotalEnrolledCount = stat.TotalEnrolled,
                    GradedStudentCount = stat.GradedCount,
                    AverageMark = courseAvg,
                    PassCount = stat.PassCount,
                    FailCount = stat.FailCount,
                    PassRate = coursePassRate
                });
            }
            else
            {
                // Course has 0 enrollments in this filtered selection
                courseAverages.Add(new CourseAverageViewModel
                {
                    CourseId = c.Id,
                    CourseCode = c.CourseCode,
                    CourseTitle = c.CourseName,
                    TeacherName = c.Teacher?.FullName ?? "Unassigned",
                    CreditHours = c.CreditHours,
                    TotalEnrolledCount = 0,
                    GradedStudentCount = 0,
                    AverageMark = 0m,
                    PassCount = 0,
                    FailCount = 0,
                    PassRate = 0m
                });
            }
        }

        // 6. Populate Filter Choices
        var yearsQuery = db.Enrollments.AsNoTracking().Select(e => e.AcademicYear).Distinct();
        var availableYears = await yearsQuery.OrderByDescending(y => y).ToListAsync();
        if (!availableYears.Any())
        {
            availableYears.Add(DateTime.Today.Year);
        }

        var availableSemesters = await db.Enrollments
            .AsNoTracking()
            .Where(e => e.Semester != null)
            .Select(e => e.Semester!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        if (!availableSemesters.Any())
        {
            availableSemesters = ["Spring", "Summer", "Fall"];
        }

        var availableDepts = await db.Students
            .AsNoTracking()
            .Where(s => !string.IsNullOrEmpty(s.Department))
            .Select(s => s.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        return new AcademicAnalyticsViewModel
        {
            Filter = filter,
            IsTeacherScope = teacherId.HasValue,
            TeacherName = teacherName,
            TotalGradedStudents = totalGraded,
            AverageFinalMark = averageMark,
            PassCount = passCount,
            FailCount = failCount,
            PassRate = passRate,
            FailRate = failRate,
            CourseAverages = courseAverages,
            AvailableAcademicYears = availableYears,
            AvailableSemesters = availableSemesters,
            AvailableDepartments = availableDepts,
            AvailableCourses = accessibleCourses
        };
    }
}
