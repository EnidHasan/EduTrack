using System.ComponentModel.DataAnnotations;

namespace EduTrack.Web.Models;

public class ClassRoutine
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Course")]
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Day of Week")]
    public string DayOfWeek { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start Time")]
    [DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Display(Name = "End Time")]
    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Room Number")]
    public string RoomNumber { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Section { get; set; }

    [Required]
    [StringLength(10)]
    public string SemesterLevel { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    [Display(Name = "Class Type")]
    public string ClassType { get; set; } = "Theory";
}

