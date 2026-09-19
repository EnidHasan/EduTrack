using EduTrack.Web.Data;
using EduTrack.Web.Models;
using EduTrack.Web.Services;
using EduTrack.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web.Controllers;

[Authorize(Policy = "AcademicStaff")]
public class ReportsController(
    AcademicAnalyticsService analyticsService,
    AtRiskEvaluationService atRiskService,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(AcademicAnalyticsFilter? filter)
    {
        filter ??= new AcademicAnalyticsFilter();
        int? teacherId = null;

        if (User.IsInRole("Teacher"))
        {
            var teacher = await CurrentTeacherAsync();
            if (teacher is null)
            {
                return Forbid();
            }

            teacherId = teacher.Id;
            // Clear department filter for teacher since teacher view is course-scoped
            filter.Department = null;

            // If a CourseId is passed in, ensure it is assigned to this teacher
            if (filter.CourseId.HasValue)
            {
                var isAssigned = await db.Courses
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == filter.CourseId.Value && c.TeacherId == teacher.Id);

                if (!isAssigned)
                {
                    // Invalid course access attempt by teacher: ignore or filter to no match
                    filter.CourseId = -1;
                }
            }
        }

        var model = await analyticsService.GetAnalyticsAsync(filter, teacherId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? department, string? semester)
    {
        var model = await atRiskService.GetSystemReportAsync(department, semester);
        var pdf = SystemReportPdfBuilder.Build(model);
        var fileName = $"EduTrack-System-Report-{DateTime.Now:yyyyMMdd-HHmm}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    private async Task<Teacher?> CurrentTeacherAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return null;
        return await db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
    }
}
