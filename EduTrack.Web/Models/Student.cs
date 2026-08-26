using System.ComponentModel.DataAnnotations;
namespace EduTrack.Web.Models;
public class Student
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, StringLength(30), Display(Name = "Student ID / Roll")] public string RollNumber { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Department { get; set; } = string.Empty;
    [Range(2000, 2100), Display(Name = "Enrollment year")] public int EnrollmentYear { get; set; } = DateTime.Today.Year;
    [StringLength(20)] public string? Semester { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
}
