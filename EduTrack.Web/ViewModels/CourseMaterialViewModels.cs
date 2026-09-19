using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EduTrack.Web.ViewModels;

public class CourseMaterialCreateViewModel
{
    [Required]
    public int CourseId { get; set; }
    
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;

    [Required, StringLength(150), Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000), Display(Name = "Description (Optional)")]
    public string? Description { get; set; }

    [Required, Display(Name = "File (PDF, DOC/DOCX, PPT/PPTX, TXT. Max 10MB)")]
    public IFormFile File { get; set; } = null!;
}

public class CourseMaterialEditViewModel
{
    public int Id { get; set; }
    
    [Required]
    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;

    [Required, StringLength(150), Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000), Display(Name = "Description (Optional)")]
    public string? Description { get; set; }
}
