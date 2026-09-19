using System.ComponentModel.DataAnnotations;

namespace EduTrack.Web.Models;

public class TeacherEvaluation
{
    public int Id { get; set; }
    public int PeriodId { get; set; }
    public TeacherEvaluationPeriod? Period { get; set; }
    public int EnrollmentId { get; set; }
    public Enrollment? Enrollment { get; set; }
    public int CourseId { get; set; }
    public int TeacherId { get; set; }
    [Required, StringLength(20)] public string CourseCode { get; set; } = string.Empty;
    [Required, StringLength(120)] public string CourseName { get; set; } = string.Empty;
    [Required, StringLength(120)] public string TeacherName { get; set; } = string.Empty;
    [Range(1, 5)] public int Clarity { get; set; }
    [Range(1, 5)] public int Preparation { get; set; }
    [Range(1, 5)] public int Fairness { get; set; }
    [Range(1, 5)] public int Communication { get; set; }
    [Range(1, 5)] public int Overall { get; set; }
    [StringLength(1000)] public string? Comment { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}
