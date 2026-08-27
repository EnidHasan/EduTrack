using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EduTrack.Web.Models;
public class Grade
{
    public int Id { get; set; }
    [Required] public int EnrollmentId { get; set; }
    public Enrollment? Enrollment { get; set; }

    [Range(0, 100), Display(Name = "Assignment marks")] public decimal AssignmentMark { get; set; }
    [Range(0, 100), Display(Name = "Attendance marks")] public decimal AttendanceMark { get; set; }
    [Range(0, 100), Display(Name = "Midterm marks")] public decimal MidtermMark { get; set; }
    [Range(0, 100), Display(Name = "Final marks")] public decimal FinalMark { get; set; }

    [Column(TypeName = "decimal(5,2)"), Display(Name = "Total mark")] public decimal TotalMark { get; set; }
    [StringLength(3), Display(Name = "Letter grade")] public string? LetterGrade { get; set; }
    [Column(TypeName = "decimal(3,2)"), Display(Name = "Grade point")] public decimal GradePoint { get; set; }

    [Display(Name = "Last updated")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
