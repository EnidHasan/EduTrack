using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EduTrack.Web.Models;
public class Teacher
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, StringLength(30), Display(Name = "Employee ID")] public string EmployeeId { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Department { get; set; } = string.Empty;
    [StringLength(80)] public string? Designation { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
    public ICollection<Course> Courses { get; set; } = [];
    [NotMapped, DataType(DataType.Password), Display(Name = "Temporary password")]
    public string? TemporaryPassword { get; set; }
}
