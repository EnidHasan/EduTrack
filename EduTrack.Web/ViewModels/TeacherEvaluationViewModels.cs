using System.ComponentModel.DataAnnotations;

namespace EduTrack.Web.ViewModels;

public class EvaluationPeriodInput
{
    [Range(2000, 2100), Display(Name = "Academic year")] public int AcademicYear { get; set; } = DateTime.Today.Year;
    [Required, RegularExpression("Fall|Spring|Summer"), Display(Name = "Term")] public string Term { get; set; } = "Fall";
    [Required, Display(Name = "Opens at (local time)")] public DateTime OpensAt { get; set; } = DateTime.Now;
    [Required, Display(Name = "Closes at (local time)")] public DateTime ClosesAt { get; set; } = DateTime.Now.AddDays(14);
}

public class EvaluationClosingDateInput
{
    public int Id { get; set; }
    [Required, Display(Name = "New closing date and time (local time)")]
    public DateTime? ClosesAt { get; set; }
}

public class TeacherEvaluationInput
{
    [Required] public int PeriodId { get; set; }
    [Required] public int EnrollmentId { get; set; }
    [Range(1, 5)] public int Clarity { get; set; }
    [Range(1, 5)] public int Preparation { get; set; }
    [Range(1, 5)] public int Fairness { get; set; }
    [Range(1, 5)] public int Communication { get; set; }
    [Range(1, 5)] public int Overall { get; set; }
    [StringLength(1000)] public string? Comment { get; set; }
}

public record EvaluationOpportunity(int PeriodId, int EnrollmentId, string PeriodLabel, string CourseCode, string CourseName, string TeacherName, bool Submitted);

public record EvaluationSummary(int PeriodId, string PeriodLabel, int CourseId, string CourseCode, string CourseName,
    int TeacherId, string TeacherName, int ResponseCount, double Clarity, double Preparation, double Fairness,
    double Communication, double Overall, bool IsVisible, IReadOnlyList<string> Comments);
