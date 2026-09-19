using System.ComponentModel.DataAnnotations;

namespace EduTrack.Web.ViewModels;

public class AcademicAnalyticsFilter
{
    [Display(Name = "Academic Year")]
    public int? AcademicYear { get; set; }

    [Display(Name = "Semester")]
    public string? Semester { get; set; }

    [Display(Name = "Department")]
    public string? Department { get; set; }

    [Display(Name = "Course")]
    public int? CourseId { get; set; }
}

public class CourseAverageViewModel
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public decimal CreditHours { get; set; }
    public int TotalEnrolledCount { get; set; }
    public int GradedStudentCount { get; set; }
    public decimal AverageMark { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal PassRate { get; set; }
}

public class AcademicAnalyticsViewModel
{
    // Filter parameters
    public AcademicAnalyticsFilter Filter { get; set; } = new();

    // Context / Scoping
    public bool IsTeacherScope { get; set; }
    public string? TeacherName { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // KPI Summary Metrics
    public int TotalGradedStudents { get; set; }
    public decimal AverageFinalMark { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal PassRate { get; set; }
    public decimal FailRate { get; set; }

    // Course Details
    public List<CourseAverageViewModel> CourseAverages { get; set; } = [];

    // Filter Dropdown Choices
    public List<int> AvailableAcademicYears { get; set; } = [];
    public List<string> AvailableSemesters { get; set; } = [];
    public List<string> AvailableDepartments { get; set; } = [];
    public List<CourseLookupItem> AvailableCourses { get; set; } = [];

    public bool HasGradedData => TotalGradedStudents > 0;
}

public class CourseLookupItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DisplayName => $"{Code} - {Title}";
}
