using System.ComponentModel.DataAnnotations;

namespace EduTrack.Web.Models;

public class TeacherEvaluationPeriod
{
    public int Id { get; set; }
    [Range(2000, 2100)] public int AcademicYear { get; set; }
    [Required, StringLength(12)] public string Term { get; set; } = string.Empty;
    public DateTime OpensAtUtc { get; set; }
    public DateTime ClosesAtUtc { get; set; }
    public bool IsEnabled { get; set; } = true;
}
