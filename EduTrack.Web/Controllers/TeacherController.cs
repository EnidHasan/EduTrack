using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduTrack.Web.Services;
namespace EduTrack.Web.Controllers;
[Authorize(Roles = "Teacher")]
public class TeacherController(ApplicationDbContext db, UserManager<ApplicationUser> users, GradeCalculatorService calculator, RecheckService recheckService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();
        var courses = await db.Courses.Where(c => c.TeacherId == teacher.Id).OrderBy(c => c.CourseCode).ToListAsync();
        return View(courses);
    }

    public async Task<IActionResult> Course(int id)
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == teacher.Id);
        if (course is null) return NotFound();
        ViewBag.Course = course;
        var enrollments = await db.Enrollments.Include(e => e.Student).Include(e => e.Grade)
            .Where(e => e.CourseId == id).OrderBy(e => e.Student!.FullName).ToListAsync();
        return View(enrollments);
    }

    public async Task<IActionResult> GradeEntry(int enrollmentId)
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();
        var enrollment = await db.Enrollments.Include(e => e.Student).Include(e => e.Course).Include(e => e.Grade)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.Course!.TeacherId == teacher.Id);
        if (enrollment is null) return NotFound();
        return View(enrollment.Grade ?? new Grade { EnrollmentId = enrollmentId, Enrollment = enrollment });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GradeEntry(Grade model)
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();
        var enrollment = await db.Enrollments.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == model.EnrollmentId && e.Course!.TeacherId == teacher.Id);
        if (enrollment is null) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var grade = await db.Grades.FirstOrDefaultAsync(g => g.EnrollmentId == model.EnrollmentId);
        if (grade is null) { grade = new Grade { EnrollmentId = model.EnrollmentId }; db.Add(grade); }
        grade.AssignmentMark = model.AssignmentMark;
        grade.AttendanceMark = model.AttendanceMark;
        grade.MidtermMark = model.MidtermMark;
        grade.FinalMark = model.FinalMark;
        calculator.Apply(grade);
        await db.SaveChangesAsync();
        TempData["Success"] = "Grade saved.";
        return RedirectToAction(nameof(Course), new { id = enrollment.CourseId });
    }

    public async Task<IActionResult> Disputes()
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();
        var requests = await recheckService.GetTeacherRequestsAsync(teacher.Id);
        return View(requests);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDisputeStatus(int requestId, string status, string? comment)
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();

        var result = await recheckService.UpdateDisputeStatusAsync(requestId, teacher.Id, status, comment);
        if (result.success) TempData["Success"] = result.message;
        else TempData["Error"] = result.message;

        return RedirectToAction(nameof(Disputes));
    }

    private async Task<Teacher?> CurrentTeacherAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return null;
        return await db.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
    }
}
