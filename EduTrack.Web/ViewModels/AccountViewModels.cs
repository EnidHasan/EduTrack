using System.ComponentModel.DataAnnotations;
using EduTrack.Web.Models;
namespace EduTrack.Web.ViewModels;
public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Display(Name = "Keep me signed in")] public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
public class UserFormViewModel
{
    public string? Id { get; set; }
    [Required, StringLength(100), Display(Name = "Full name")] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = "Student";
    [DataType(DataType.Password), Display(Name = "Temporary password")] public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsRoleLocked { get; set; }
    public string LinkedRecord { get; set; } = "Administration";
}
public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Current password")] public string CurrentPassword { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8), Display(Name = "New password")] public string NewPassword { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(NewPassword)), Display(Name = "Confirm new password")] public string ConfirmPassword { get; set; } = string.Empty;
    public bool IsFirstLogin { get; set; }
}
public class ProfileViewModel
{
    [Required, StringLength(100), Display(Name = "Full name")] public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    [Phone, Display(Name = "Phone number")] public string? PhoneNumber { get; set; }
}
public record UserListItem(string Id, string FullName, string Email, string Role, bool IsActive, DateTime CreatedAt, string Source, bool IsLinked, bool IsCurrent);
public record DashboardViewModel(int Students, int Teachers, int Courses, int Users, IReadOnlyList<Course> RecentCourses);
