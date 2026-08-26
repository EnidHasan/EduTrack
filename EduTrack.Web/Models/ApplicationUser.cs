using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace EduTrack.Web.Models;
public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [StringLength(20)] public string? ProfileType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
}
