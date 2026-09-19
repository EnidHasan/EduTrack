using EduTrack.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduTrack.Web.Controllers;

[Authorize(Policy = "AcademicStaff")]
public class AnalyticsController : Controller
{
    public IActionResult Index(AcademicAnalyticsFilter? filter)
    {
        return RedirectToAction("Index", "Reports", filter);
    }
}
