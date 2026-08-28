using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EduTrack.Web.Models;
public class Grade
{
    public int Id { get; set; }
    [Required] public int EnrollmentId { get; set; }
    public Enrollment? Enrollment { get; set; }

    [Range(0, 20), Column(TypeName = "decimal(5,2)"), Display(Name = "Quiz marks (out of 20)")] public decimal AssignmentMark { get; set; }
    [Range(0, 10), Column(TypeName = "decimal(5,2)"), Display(Name = "Attendance marks (out of 10)")] public decimal AttendanceMark { get; set; }
    [Range(0, 20), Column(TypeName = "decimal(5,2)"), Display(Name = "Midterm marks (out of 20)")] public decimal MidtermMark { get; set; }
    [Range(0, 50), Column(TypeName = "decimal(5,2)"), Display(Name = "Final marks (out of 50)")] public decimal FinalMark { get; set; }

    [Column(TypeName = "decimal(5,2)"), Display(Name = "Total mark")] public decimal TotalMark { get; set; }
    [StringLength(3), Display(Name = "Letter grade")] public string? LetterGrade { get; set; }
    [Column(TypeName = "decimal(3,2)"), Display(Name = "Grade point")] public decimal GradePoint { get; set; }

    [Display(Name = "Last updated")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
