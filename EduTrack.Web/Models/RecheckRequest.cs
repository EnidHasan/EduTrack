using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduTrack.Web.Models;

public class RecheckRequest
{
    public int Id { get; set; }

    [Required]
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    [Required]
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    [Required]
    public int GradeId { get; set; }
    public Grade? Grade { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    [StringLength(500)]
    [Display(Name = "Teacher Comment")]
    public string? TeacherComment { get; set; }

    [Display(Name = "Request Date")]
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
}
