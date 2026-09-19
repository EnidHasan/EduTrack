using EduTrack.Web.Data;
using EduTrack.Web.Models;
using EduTrack.Web.Services;
using EduTrack.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web.Controllers;

[Authorize]
public class CourseMaterialsController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> users,
    CourseMaterialService materialService) : Controller
{
    private async Task<Teacher?> CurrentTeacherAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return null;
        return await db.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
    }

    private async Task<Student?> CurrentStudentAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return null;
        return await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id);
    }

    [Authorize(Policy = "TeacherOnly")]
    public async Task<IActionResult> TeacherIndex(int courseId)
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);
        if (course is null) return NotFound();

        ViewBag.Course = course;
        var materials = await materialService.GetMaterialsForCourseAsync(courseId);
        return View(materials);
    }

    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> StudentIndex(int courseId)
    {
        var student = await CurrentStudentAsync();
        if (student is null) return Forbid();

        if (!await materialService.CanStudentAccessCourseAsync(student.Id, courseId))
        {
            return Forbid();
        }

        var course = await db.Courses.FindAsync(courseId);
        ViewBag.Course = course;
        var materials = await materialService.GetMaterialsForCourseAsync(courseId);
        return View(materials);
    }

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdminIndex()
    {
        var materials = await db.CourseMaterials
            .Include(m => m.Course)
            .Include(m => m.Teacher)
            .OrderByDescending(m => m.UploadedAt)
            .ToListAsync();
        return View(materials);
    }

    [Authorize(Policy = "TeacherOnly")]
    public async Task<IActionResult> Create(int courseId)
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);
        if (course is null) return NotFound();

        var model = new CourseMaterialCreateViewModel
        {
            CourseId = course.Id,
            CourseCode = course.CourseCode,
            CourseName = course.CourseName
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = "TeacherOnly")]
    public async Task<IActionResult> Create(CourseMaterialCreateViewModel model)
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();

        if (!await materialService.CanTeacherManageCourseAsync(teacher.Id, model.CourseId))
            return Forbid();

        if (!ModelState.IsValid)
        {
            var course = await db.Courses.FindAsync(model.CourseId);
            model.CourseCode = course?.CourseCode ?? "";
            model.CourseName = course?.CourseName ?? "";
            return View(model);
        }

        var result = await materialService.UploadMaterialAsync(model.CourseId, teacher.Id, model.Title, model.Description, model.File);
        
        if (result.success)
        {
            TempData["Success"] = result.message;
            return RedirectToAction(nameof(TeacherIndex), new { courseId = model.CourseId });
        }

        TempData["Error"] = result.message;
        var retryCourse = await db.Courses.FindAsync(model.CourseId);
        model.CourseCode = retryCourse?.CourseCode ?? "";
        model.CourseName = retryCourse?.CourseName ?? "";
        return View(model);
    }

    [Authorize(Policy = "TeacherOnly")]
    public async Task<IActionResult> Edit(int id)
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();

        var material = await materialService.GetMaterialAsync(id);
        if (material is null || material.TeacherId != teacher.Id) return NotFound();

        var model = new CourseMaterialEditViewModel
        {
            Id = material.Id,
            CourseId = material.CourseId,
            CourseCode = material.Course!.CourseCode,
            CourseName = material.Course!.CourseName,
            Title = material.Title,
            Description = material.Description
        };

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = "TeacherOnly")]
    public async Task<IActionResult> Edit(CourseMaterialEditViewModel model)
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();

        var material = await materialService.GetMaterialAsync(model.Id);
        if (material is null || material.TeacherId != teacher.Id) return NotFound();

        if (!ModelState.IsValid) return View(model);

        material.Title = model.Title;
        material.Description = model.Description;
        material.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        TempData["Success"] = "Material updated successfully.";
        
        return RedirectToAction(nameof(TeacherIndex), new { courseId = material.CourseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = "TeacherOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();

        var material = await materialService.GetMaterialAsync(id);
        if (material is null || material.TeacherId != teacher.Id) return NotFound();

        var courseId = material.CourseId;
        var result = await materialService.DeleteMaterialAsync(id);

        if (result.success) TempData["Success"] = result.message;
        else TempData["Error"] = result.message;

        return RedirectToAction(nameof(TeacherIndex), new { courseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdminDelete(int id)
    {
        var material = await materialService.GetMaterialAsync(id);
        if (material is null) return NotFound();

        var result = await materialService.DeleteMaterialAsync(id);

        if (result.success) TempData["Success"] = result.message;
        else TempData["Error"] = result.message;

        return RedirectToAction(nameof(AdminIndex));
    }

    [Authorize]
    public async Task<IActionResult> Download(int id)
    {
        var material = await materialService.GetMaterialAsync(id);
        if (material is null || !material.IsActive) return NotFound();

        if (User.IsInRole("Admin"))
        {
            // Admins can download anything
        }
        else if (User.IsInRole("Teacher"))
        {
            var teacher = await CurrentTeacherAsync();
            if (teacher is null || material.TeacherId != teacher.Id) return Forbid();
        }
        else if (User.IsInRole("Student"))
        {
            var student = await CurrentStudentAsync();
            if (student is null || !await materialService.CanStudentAccessCourseAsync(student.Id, material.CourseId))
                return Forbid();
        }
        else
        {
            return Forbid();
        }

        if (!System.IO.File.Exists(material.FilePath))
        {
            return NotFound("The requested file was not found on the server.");
        }

        return PhysicalFile(material.FilePath, material.ContentType, material.OriginalFileName);
    }
}
