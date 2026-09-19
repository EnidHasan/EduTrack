using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace EduTrack.Web.Services;

public class CourseMaterialService(ApplicationDbContext db, IWebHostEnvironment env, ILogger<CourseMaterialService> logger)
{
    private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".txt" };
    private readonly string[] _allowedMimeTypes = { 
        "application/pdf", 
        "application/msword", 
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain" 
    };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    
    // Using a dedicated folder for course materials
    private string StorageDirectory => Path.Combine(env.ContentRootPath, "App_Data", "course-materials");

    public async Task<bool> CanTeacherManageCourseAsync(int teacherId, int courseId)
    {
        return await db.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == teacherId);
    }

    public async Task<bool> CanStudentAccessCourseAsync(int studentId, int courseId)
    {
        return await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId && e.IsActive);
    }

    public async Task<(bool success, string message, CourseMaterial? material)> UploadMaterialAsync(
        int courseId, int teacherId, string title, string? description, IFormFile file)
    {
        if (file == null || file.Length == 0) return (false, "File is empty or missing.", null);
        if (file.Length > MaxFileSize) return (false, "File size exceeds 10MB limit.", null);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension)) return (false, "Invalid file type.", null);
        if (!_allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant())) return (false, "Invalid MIME type.", null);

        if (!Directory.Exists(StorageDirectory))
        {
            Directory.CreateDirectory(StorageDirectory);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var physicalPath = Path.Combine(StorageDirectory, uniqueFileName);

        try
        {
            using var stream = new FileStream(physicalPath, FileMode.Create);
            await file.CopyToAsync(stream);

            var material = new CourseMaterial
            {
                CourseId = courseId,
                TeacherId = teacherId,
                Title = title,
                Description = description,
                OriginalFileName = Path.GetFileName(file.FileName),
                StoredFileName = uniqueFileName,
                FilePath = physicalPath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow,
                IsActive = true
            };

            db.CourseMaterials.Add(material);
            await db.SaveChangesAsync();

            return (true, "Material uploaded successfully.", material);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading course material.");
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath); // Cleanup on DB failure
            }
            return (false, "An error occurred during upload.", null);
        }
    }

    public async Task<(bool success, string message)> DeleteMaterialAsync(int materialId)
    {
        var material = await db.CourseMaterials.FindAsync(materialId);
        if (material == null) return (false, "Material not found.");

        try
        {
            if (File.Exists(material.FilePath))
            {
                File.Delete(material.FilePath);
            }

            db.CourseMaterials.Remove(material);
            await db.SaveChangesAsync();
            return (true, "Material deleted successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting course material.");
            return (false, "An error occurred while deleting the material.");
        }
    }

    public async Task<List<CourseMaterial>> GetMaterialsForCourseAsync(int courseId)
    {
        return await db.CourseMaterials
            .Where(m => m.CourseId == courseId && m.IsActive)
            .OrderByDescending(m => m.UploadedAt)
            .ToListAsync();
    }
    
    public async Task<CourseMaterial?> GetMaterialAsync(int id)
    {
        return await db.CourseMaterials.Include(m => m.Course).FirstOrDefaultAsync(m => m.Id == id);
    }
}
