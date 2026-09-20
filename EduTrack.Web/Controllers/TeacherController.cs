using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduTrack.Web.Services;
namespace EduTrack.Web.Controllers;
[Authorize(Policy = "TeacherOnly")]
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
        var enrollment = await db.Enrollments.Include(e => e.Course).Include(e => e.Student).FirstOrDefaultAsync(e => e.Id == model.EnrollmentId && e.Course!.TeacherId == teacher.Id);
        if (enrollment is null) return NotFound();

        // Explicit server-side range validation
        if (model.AssignmentMark < 0 || model.AssignmentMark > 20)
            ModelState.AddModelError(nameof(model.AssignmentMark), "Quiz marks must be between 0 and 20.");
        if (model.AttendanceMark < 0 || model.AttendanceMark > 10)
            ModelState.AddModelError(nameof(model.AttendanceMark), "Attendance marks must be between 0 and 10.");
        if (model.MidtermMark < 0 || model.MidtermMark > 20)
            ModelState.AddModelError(nameof(model.MidtermMark), "Midterm marks must be between 0 and 20.");
        if (model.FinalMark < 0 || model.FinalMark > 50)
            ModelState.AddModelError(nameof(model.FinalMark), "Final marks must be between 0 and 50.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please correct the validation errors and try again.";
            return View(model);
        }

        var grade = await db.Grades.FirstOrDefaultAsync(g => g.EnrollmentId == model.EnrollmentId);
        if (grade is null) { grade = new Grade { EnrollmentId = model.EnrollmentId }; db.Add(grade); }
        grade.AssignmentMark = model.AssignmentMark;
        grade.AttendanceMark = model.AttendanceMark;
        grade.MidtermMark = model.MidtermMark;
        grade.FinalMark = model.FinalMark;
        calculator.Apply(grade);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Grade saved successfully for {enrollment.Student?.FullName ?? "student"} — Total: {grade.TotalMark}, Grade: {grade.LetterGrade}.";
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

    public async Task<IActionResult> MyRoutine()
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();

        var routines = await db.ClassRoutines
            .Include(r => r.Course)
            .Where(r => r.Course!.TeacherId == teacher.Id)
            .ToListAsync();

        return View(routines);
    }

    public async Task<IActionResult> DownloadRoutinePdf()
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();

        var routines = await db.ClassRoutines
            .Include(r => r.Course).ThenInclude(c => c!.Teacher)
            .Where(r => r.Course!.TeacherId == teacher.Id)
            .ToListAsync();
        var pdf = ClassRoutinePdfBuilder.Build(routines, $"Teacher: {teacher.FullName}");
        return File(pdf, "application/pdf", "EduTrack-Teacher-Routine.pdf");
    }

    private async Task<Teacher?> CurrentTeacherAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return null;
        return await db.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
    }
}
