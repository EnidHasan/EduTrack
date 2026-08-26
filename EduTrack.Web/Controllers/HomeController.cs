using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EduTrack.Web.Models;

namespace EduTrack.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Dashboard") : RedirectToAction("Login", "Account");

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
