using System.ComponentModel.DataAnnotations;
namespace EduTrack.Web.Models;
public class Course
{
    public int Id { get; set; }
    [Required, StringLength(20), Display(Name = "Course code")] public string CourseCode { get; set; } = string.Empty;
    [Required, StringLength(120), Display(Name = "Course title")] public string CourseName { get; set; } = string.Empty;
    [Range(0.5, 6), Display(Name = "Credit hours")] public decimal CreditHours { get; set; }
    [StringLength(30)] public string? Semester { get; set; }
    [Display(Name = "Assigned teacher")] public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public bool IsActive { get; set; } = true;
    [Display(Name = "Final Exam Date"), DataType(DataType.Date)] public DateTime? FinalExamDate { get; set; }
    [Display(Name = "Final Exam Time"), StringLength(50)] public string? FinalExamTime { get; set; }
    public ICollection<CourseMaterial> CourseMaterials { get; set; } = [];
}
