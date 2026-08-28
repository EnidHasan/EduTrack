using System.ComponentModel.DataAnnotations;
namespace EduTrack.Web.Models;
public class Enrollment
{
    public int Id { get; set; }
    [Required] public int StudentId { get; set; }
    public Student? Student { get; set; }
    [Required] public int CourseId { get; set; }
    public Course? Course { get; set; }
    [StringLength(30)] public string? Semester { get; set; }
    [Range(2000, 2100), Display(Name = "Academic year")] public int AcademicYear { get; set; } = DateTime.Today.Year;
    [Display(Name = "Enrolled on")] public DateTime EnrolledOn { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public Grade? Grade { get; set; }
}
