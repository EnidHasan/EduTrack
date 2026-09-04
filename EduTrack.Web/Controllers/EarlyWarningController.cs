using System.Text;
using EduTrack.Web.Data;
using EduTrack.Web.Models;
using EduTrack.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web.Controllers;

[Authorize(Policy = "AcademicStaff")]
public class EarlyWarningController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    AtRiskEvaluationService atRiskService) : Controller
{
    public async Task<IActionResult> Index(
        string? department,
        string? riskLevel,
        string? riskType,
        string? search)
    {
        int? teacherId = null;

        if (User.IsInRole("Teacher"))
        {
            var user = await userManager.GetUserAsync(User);
            if (user != null)
            {
                var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id);
                if (teacher != null)
                {
                    teacherId = teacher.Id;
                }
            }
        }

        // Sync flags into DB in background if needed
        try { await atRiskService.SyncAtRiskFlagsAsync(); } catch { /* Non-blocking */ }

        var model = await atRiskService.GetAtRiskWarningPanelAsync(
            department: department,
            riskLevel: riskLevel,
            riskType: riskType,
            search: search,
            teacherId: teacherId);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(
        string? department,
        string? riskLevel,
        string? riskType,
        string? search)
    {
        int? teacherId = null;
        if (User.IsInRole("Teacher"))
        {
            var user = await userManager.GetUserAsync(User);
            var teacher = user != null ? await db.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == user.Id) : null;
            if (teacher != null) teacherId = teacher.Id;
        }

        var model = await atRiskService.GetAtRiskWarningPanelAsync(
            department: department,
            riskLevel: riskLevel,
            riskType: riskType,
            search: search,
            teacherId: teacherId);

        var builder = new StringBuilder();
        builder.AppendLine("Student ID / Roll,Full Name,Email,Department,Semester,Running GPA,Attendance %,Risk Level,Risk Type,Reason");

        foreach (var item in model.AtRiskStudents)
        {
            builder.AppendLine($"\"{item.RollNumber}\",\"{item.FullName}\",\"{item.Email}\",\"{item.Department}\",\"{item.Semester ?? ""}\",{item.RunningGpa:F2},{item.AttendancePercent:F1}%,{item.RiskLevel},{item.RiskType},\"{item.Reason}\"");
        }

        var csvBytes = Encoding.UTF8.GetBytes(builder.ToString());
        return File(csvBytes, "text/csv", $"EduTrack_AtRisk_Students_{DateTime.Now:yyyyMMdd}.csv");
    }
}
