using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduTrack.Web.Models;

public class AtRiskFlag
{
    public int Id { get; set; }

    [Required]
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    [Column(TypeName = "decimal(3,2)")]
    [Display(Name = "Running GPA")]
    public decimal RunningGpa { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Display(Name = "Attendance %")]
    public decimal AttendancePercent { get; set; }

    [Required, StringLength(30)]
    public string RiskLevel { get; set; } = "Normal"; // High, Medium, Normal

    [Required, StringLength(30)]
    public string RiskType { get; set; } = "None"; // Academic, Attendance, Both, None

    [StringLength(250)]
    public string? Reason { get; set; }

    [StringLength(50)]
    public string? Semester { get; set; }

    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}
