using EduTrack.Web.Data;
using EduTrack.Web.Models;
using EduTrack.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web.Controllers;

[Authorize(Policy = "StudentOnly")]
public class StudentController(ApplicationDbContext db, UserManager<ApplicationUser> users, RecheckService recheckService, AtRiskEvaluationService atRiskService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return NotFound();

        var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id);
        if (student == null) return NotFound();

        var atRiskEval = await atRiskService.EvaluateStudentRiskAsync(student.Id);
        ViewBag.AtRiskEval = atRiskEval;

        var enrollments = await db.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Grade)
            .Where(e => e.StudentId == student.Id)
            .OrderByDescending(e => e.AcademicYear)
            .ThenByDescending(e => e.Semester)
            .ToListAsync();

        decimal totalCredits = enrollments
            .Where(e => e.Grade != null && e.Course != null)
            .Sum(e => e.Course!.CreditHours);

        var gpaBySemester = enrollments
            .Where(e => e.Grade != null && e.Course != null)
            .GroupBy(e => new { e.AcademicYear, e.Semester })
            .Select(g => new
            {
                Semester = FormatAcademicTerm(g.Key.Semester, g.Key.AcademicYear),
                GPA = Math.Round(g.Sum(e => e.Grade!.GradePoint * e.Course!.CreditHours) / g.Sum(e => e.Course!.CreditHours), 2)
            })
            .ToDictionary(k => k.Semester, v => v.GPA);

        ViewBag.CGPA = student.CGPA;
        ViewBag.TotalCredits = totalCredits;
        ViewBag.GPABySemester = gpaBySemester;
        ViewBag.StudentId = student.Id;

        return View(enrollments);
    }

    private static string FormatAcademicTerm(string? semester, int academicYear)
    {
        var term = semester?.Trim() ?? string.Empty;
        var year = academicYear.ToString();
        if (string.IsNullOrWhiteSpace(term)) return year;
        return term.EndsWith(year, StringComparison.OrdinalIgnoreCase) ? term : $"{term} {year}";
    }

    public async Task<IActionResult> Disputes()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return NotFound();

        var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id);
        if (student == null) return NotFound();

        var requests = await recheckService.GetStudentRequestsAsync(student.Id);
        return View(requests);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitDispute(int gradeId)
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return NotFound();

        var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id);
        if (student == null) return NotFound();

        var result = await recheckService.SubmitDisputeAsync(student.Id, gradeId);
        
        if (result.success)
            TempData["Success"] = result.message;
        else
            TempData["Error"] = result.message;

        return RedirectToAction(nameof(Disputes));
    }

    public async Task<IActionResult> MyRoutine()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return NotFound();

        var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id);
        if (student == null) return NotFound();

        var enrolledCourseIds = await db.Enrollments
            .Where(e => e.StudentId == student.Id)
            .Select(e => e.CourseId)
            .ToListAsync();

        var routines = await db.ClassRoutines
            .Include(r => r.Course)
            .ThenInclude(c => c!.Teacher)
            .Where(r => enrolledCourseIds.Contains(r.CourseId))
            .ToListAsync();

        return View(routines);
    }

    public async Task<IActionResult> DownloadRoutinePdf()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return NotFound();
        var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id);
        if (student is null) return NotFound();

        var enrolledCourseIds = await db.Enrollments
            .Where(e => e.StudentId == student.Id)
            .Select(e => e.CourseId)
            .ToListAsync();
        var routines = await db.ClassRoutines
            .Include(r => r.Course).ThenInclude(c => c!.Teacher)
            .Where(r => enrolledCourseIds.Contains(r.CourseId))
            .ToListAsync();
        var pdf = ClassRoutinePdfBuilder.Build(routines, $"Student: {student.FullName} ({student.RollNumber})");
        return File(pdf, "application/pdf", "EduTrack-Student-Routine.pdf");
    }

    public async Task<IActionResult> ExamRoutine()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return NotFound();

        var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id);
        if (student == null) return NotFound();

        var enrolledCourses = await db.Enrollments
            .Include(e => e.Course)
            .ThenInclude(c => c!.Teacher)
            .Where(e => e.StudentId == student.Id && e.Course != null)
            .Select(e => e.Course!)
            .Distinct()
            .OrderBy(c => c.FinalExamDate.HasValue ? 0 : 1)
            .ThenBy(c => c.FinalExamDate)
            .ThenBy(c => c.CourseCode)
            .ToListAsync();

        return View(enrolledCourses);
    }

    public async Task<IActionResult> DownloadExamRoutinePdf()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return NotFound();
        var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id);
        if (student is null) return NotFound();

        var enrolledCourses = await db.Enrollments
            .Include(e => e.Course)
            .ThenInclude(c => c!.Teacher)
            .Where(e => e.StudentId == student.Id && e.Course != null)
            .Select(e => e.Course!)
            .Distinct()
            .OrderBy(c => c.FinalExamDate.HasValue ? 0 : 1)
            .ThenBy(c => c.FinalExamDate)
            .ThenBy(c => c.CourseCode)
            .ToListAsync();

        var pdf = ExamRoutinePdfBuilder.Build(enrolledCourses, $"Student: {student.FullName} ({student.RollNumber})");
        return File(pdf, "application/pdf", $"EduTrack-Exam-Routine-{student.RollNumber}.pdf");
    }
}
