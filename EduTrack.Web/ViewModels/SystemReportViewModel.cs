namespace EduTrack.Web.ViewModels;

public class DepartmentSummaryItem
{
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int AtRiskCount { get; set; }
    public int HighRiskCount { get; set; }
    public int MediumRiskCount { get; set; }
    public decimal AverageGpa { get; set; }
    public decimal AverageAttendance { get; set; }
    public decimal AtRiskPercentage => TotalStudents > 0 ? Math.Round((decimal)AtRiskCount / TotalStudents * 100, 1) : 0;
}

public class CourseSummaryReportItem
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string TeacherName { get; set; } = "Unassigned";
    public decimal CreditHours { get; set; }
    public int TotalEnrolled { get; set; }
    public int GradedCount { get; set; }
    public decimal AverageTotalMark { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal PassRatePercent => GradedCount > 0 ? Math.Round((decimal)PassCount / GradedCount * 100, 1) : 0;
    public int AtRiskEnrolledCount { get; set; }
}

public class SystemReportViewModel
{
    public int TotalStudentsCount { get; set; }
    public int TotalTeachersCount { get; set; }
    public int TotalCoursesCount { get; set; }
    public int TotalEnrollmentsCount { get; set; }
    public int TotalGradesCount { get; set; }
    
    public int TotalAtRiskCount { get; set; }
    public int HighRiskCount { get; set; }
    public int MediumRiskCount { get; set; }
    public int NormalCount { get; set; }
    
    public int AcademicRiskCount { get; set; }
    public int AttendanceRiskCount { get; set; }
    public int BothRiskCount { get; set; }
    
    public decimal SystemAverageGpa { get; set; }
    public decimal SystemAverageAttendance { get; set; }
    public int StudentsBelowGpaThresholdCount { get; set; }
    public int StudentsBelowAttendanceThresholdCount { get; set; }
    
    public List<DepartmentSummaryItem> DepartmentSummaries { get; set; } = new();
    public List<CourseSummaryReportItem> CourseSummaries { get; set; } = new();
    
    // Filters
    public string? SelectedDepartment { get; set; }
    public string? SelectedSemester { get; set; }
    public List<string> AvailableDepartments { get; set; } = new();
    public List<string> AvailableSemesters { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}
