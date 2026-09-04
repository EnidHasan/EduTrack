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

        decimal totalPoints = 0;
        decimal totalCredits = 0;

        foreach (var enrollment in enrollments)
        {
            if (enrollment.Grade != null && enrollment.Course != null)
            {
                totalPoints += enrollment.Grade.GradePoint * enrollment.Course.CreditHours;
                totalCredits += enrollment.Course.CreditHours;
            }
        }

        decimal cgpa = totalCredits > 0 ? Math.Round(totalPoints / totalCredits, 2) : 0;

        var gpaBySemester = enrollments
            .Where(e => e.Grade != null && e.Course != null)
            .GroupBy(e => new { e.AcademicYear, e.Semester })
            .Select(g => new
            {
                Semester = $"{g.Key.Semester} {g.Key.AcademicYear}",
                GPA = Math.Round(g.Sum(e => e.Grade!.GradePoint * e.Course!.CreditHours) / g.Sum(e => e.Course!.CreditHours), 2)
            })
            .ToDictionary(k => k.Semester, v => v.GPA);

        ViewBag.CGPA = cgpa;
        ViewBag.TotalCredits = totalCredits;
        ViewBag.GPABySemester = gpaBySemester;
        ViewBag.StudentId = student.Id;

        return View(enrollments);
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
}
