using EduTrack.Web.Data;
using EduTrack.Web.Models;
using EduTrack.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web.Controllers;

[Authorize]
public class TeacherEvaluationsController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Index()
    {
        var student = await CurrentStudentAsync();
        if (student is null) return Forbid();
        var now = DateTime.UtcNow;
        var periods = await db.TeacherEvaluationPeriods.AsNoTracking()
            .Where(p => p.IsEnabled && p.OpensAtUtc <= now && p.ClosesAtUtc > now).ToListAsync();
        var enrollments = await db.Enrollments.AsNoTracking().Include(e => e.Course).ThenInclude(c => c!.Teacher)
            .Where(e => e.StudentId == student.Id && e.IsActive && e.Course!.TeacherId != null).ToListAsync();
        var submitted = await db.TeacherEvaluations.AsNoTracking()
            .Where(e => e.Enrollment!.StudentId == student.Id)
            .Select(e => new { e.PeriodId, e.EnrollmentId }).ToListAsync();
        var model = (from period in periods
                     from enrollment in enrollments
                     where Matches(period, enrollment)
                     select new EvaluationOpportunity(period.Id, enrollment.Id, $"{period.Term} {period.AcademicYear}",
                         enrollment.Course!.CourseCode, enrollment.Course.CourseName,
                         enrollment.Course.Teacher?.FullName ?? "Teacher",
                         submitted.Any(x => x.PeriodId == period.Id && x.EnrollmentId == enrollment.Id)))
            .OrderBy(x => x.CourseCode).ToList();
        return View(model);
    }

    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Submit(int periodId, int enrollmentId)
    {
        var student = await CurrentStudentAsync();
        if (student is null) return Forbid();
        var target = await EligibleTargetAsync(student.Id, periodId, enrollmentId);
        if (target is null) return NotFound();
        if (await db.TeacherEvaluations.AnyAsync(x => x.PeriodId == periodId && x.EnrollmentId == enrollmentId))
        {
            TempData["Error"] = "You have already submitted this evaluation.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Course = target.Value.Enrollment.Course;
        ViewBag.Period = target.Value.Period;
        return View(new TeacherEvaluationInput { PeriodId = periodId, EnrollmentId = enrollmentId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Submit(TeacherEvaluationInput input)
    {
        var student = await CurrentStudentAsync();
        if (student is null) return Forbid();
        var target = await EligibleTargetAsync(student.Id, input.PeriodId, input.EnrollmentId);
        if (target is null) return NotFound();
        if (await db.TeacherEvaluations.AnyAsync(x => x.PeriodId == input.PeriodId && x.EnrollmentId == input.EnrollmentId))
        {
            TempData["Error"] = "You have already submitted this evaluation.";
            return RedirectToAction(nameof(Index));
        }
        if (!ModelState.IsValid)
        {
            ViewBag.Course = target.Value.Enrollment.Course;
            ViewBag.Period = target.Value.Period;
            return View(input);
        }
        db.TeacherEvaluations.Add(new TeacherEvaluation
        {
            PeriodId = input.PeriodId, EnrollmentId = input.EnrollmentId,
            CourseId = target.Value.Enrollment.CourseId,
            TeacherId = target.Value.Enrollment.Course!.TeacherId!.Value,
            CourseCode = target.Value.Enrollment.Course.CourseCode,
            CourseName = target.Value.Enrollment.Course.CourseName,
            TeacherName = target.Value.Enrollment.Course.Teacher?.FullName ?? "Teacher",
            Clarity = input.Clarity, Preparation = input.Preparation, Fairness = input.Fairness,
            Communication = input.Communication, Overall = input.Overall,
            Comment = string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim()
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            TempData["Error"] = "This evaluation could not be saved. It may already have been submitted.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Thank you. Your evaluation was submitted anonymously.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Admin()
    {
        ViewBag.Periods = await db.TeacherEvaluationPeriods.AsNoTracking()
            .OrderByDescending(p => p.AcademicYear).ThenByDescending(p => p.OpensAtUtc).ToListAsync();
        return View(await BuildSummariesAsync(null, true));
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult CreatePeriod() => View(new EvaluationPeriodInput());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreatePeriod(EvaluationPeriodInput input)
    {
        if (input.ClosesAt <= input.OpensAt) ModelState.AddModelError(nameof(input.ClosesAt), "Closing time must be after opening time.");
        if (await db.TeacherEvaluationPeriods.AnyAsync(p => p.AcademicYear == input.AcademicYear && p.Term == input.Term))
            ModelState.AddModelError(nameof(input.Term), "An evaluation period already exists for this term and year.");
        if (!ModelState.IsValid) return View(input);
        db.TeacherEvaluationPeriods.Add(new TeacherEvaluationPeriod
        {
            AcademicYear = input.AcademicYear, Term = input.Term,
            OpensAtUtc = input.OpensAt.ToUniversalTime(), ClosesAtUtc = input.ClosesAt.ToUniversalTime()
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The period could not be saved. Check for a duplicate term and year.");
            return View(input);
        }
        TempData["Success"] = "Evaluation period created.";
        return RedirectToAction(nameof(Admin));
    }

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> EditClosingDate(int id)
    {
        var period = await db.TeacherEvaluationPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (period is null) return NotFound();
        ViewBag.Period = period;
        return View(new EvaluationClosingDateInput { Id = id, ClosesAt = period.ClosesAtUtc.ToLocalTime() });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> EditClosingDate(EvaluationClosingDateInput input)
    {
        var period = await db.TeacherEvaluationPeriods.FindAsync(input.Id);
        if (period is null) return NotFound();
        ViewBag.Period = period;
        if (input.ClosesAt.HasValue && input.ClosesAt.Value.ToUniversalTime() <= period.OpensAtUtc)
            ModelState.AddModelError(nameof(input.ClosesAt), "Closing time must be after the opening time.");
        if (!ModelState.IsValid) return View(input);

        period.ClosesAtUtc = input.ClosesAt!.Value.ToUniversalTime();
        await db.SaveChangesAsync();
        TempData["Success"] = "Evaluation closing date updated.";
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> TogglePeriod(int id)
    {
        var period = await db.TeacherEvaluationPeriods.FindAsync(id);
        if (period is null) return NotFound();
        period.IsEnabled = !period.IsEnabled;
        await db.SaveChangesAsync();
        TempData["Success"] = period.IsEnabled ? "Evaluation period enabled." : "Evaluation period paused.";
        return RedirectToAction(nameof(Admin));
    }

    [Authorize(Policy = "TeacherOnly")]
    public async Task<IActionResult> Teacher()
    {
        var teacher = await CurrentTeacherAsync();
        if (teacher is null) return Forbid();
        return View(await BuildSummariesAsync(teacher.Id, false));
    }

    private async Task<List<EvaluationSummary>> BuildSummariesAsync(int? teacherId, bool admin)
    {
        var responses = await db.TeacherEvaluations.AsNoTracking()
            .Include(e => e.Period)
            .Where(e => teacherId == null || e.TeacherId == teacherId).ToListAsync();
        return responses.GroupBy(e => new { e.PeriodId, e.CourseId, e.TeacherId })
            .Select(g =>
            {
                var first = g.First();
                var visible = admin || (first.Period!.ClosesAtUtc <= DateTime.UtcNow && g.Count() >= 3);
                return new EvaluationSummary(first.PeriodId, $"{first.Period!.Term} {first.Period.AcademicYear}",
                    first.CourseId, first.CourseCode, first.CourseName,
                    first.TeacherId, first.TeacherName, g.Count(),
                    visible ? g.Average(x => x.Clarity) : 0, visible ? g.Average(x => x.Preparation) : 0,
                    visible ? g.Average(x => x.Fairness) : 0, visible ? g.Average(x => x.Communication) : 0,
                    visible ? g.Average(x => x.Overall) : 0, visible,
                    visible ? g.Where(x => !string.IsNullOrWhiteSpace(x.Comment)).Select(x => x.Comment!).ToList() : []);
            })
            .OrderByDescending(x => x.PeriodLabel).ThenBy(x => x.CourseCode).ToList();
    }

    private async Task<(TeacherEvaluationPeriod Period, Enrollment Enrollment)?> EligibleTargetAsync(int studentId, int periodId, int enrollmentId)
    {
        var now = DateTime.UtcNow;
        var period = await db.TeacherEvaluationPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId && p.IsEnabled && p.OpensAtUtc <= now && p.ClosesAtUtc > now);
        if (period is null) return null;
        var enrollment = await db.Enrollments.AsNoTracking().Include(e => e.Course).ThenInclude(c => c!.Teacher)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == studentId && e.IsActive && e.Course!.TeacherId != null);
        return enrollment is not null && Matches(period, enrollment) ? (period, enrollment) : null;
    }

    private static bool Matches(TeacherEvaluationPeriod period, Enrollment enrollment) =>
        enrollment.AcademicYear == period.AcademicYear &&
        (string.Equals(enrollment.Semester?.Trim(), period.Term, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(enrollment.Semester?.Trim(), $"{period.Term} {period.AcademicYear}", StringComparison.OrdinalIgnoreCase));

    private async Task<Student?> CurrentStudentAsync()
    {
        var user = await users.GetUserAsync(User);
        return user is null ? null : await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id);
    }

    private async Task<Teacher?> CurrentTeacherAsync()
    {
        var user = await users.GetUserAsync(User);
        return user is null ? null : await db.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
    }
}
