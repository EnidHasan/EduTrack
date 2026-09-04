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

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? department, string? semester)
    {
        var model = await atRiskService.GetSystemReportAsync(department, semester);
        var pdf = SystemReportPdfBuilder.Build(model);
        var fileName = $"EduTrack-System-Report-{DateTime.Now:yyyyMMdd-HHmm}.pdf";
        return File(pdf, "application/pdf", fileName);
    }
}
