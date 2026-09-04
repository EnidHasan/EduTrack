using EduTrack.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduTrack.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class ReportsController(AtRiskEvaluationService atRiskService) : Controller
{
    public async Task<IActionResult> Index(string? department, string? semester)
    {
        var model = await atRiskService.GetSystemReportAsync(department, semester);
        return View(model);
    }
}
