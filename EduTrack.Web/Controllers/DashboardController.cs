using EduTrack.Web.Data;
using EduTrack.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Controllers;
[Authorize]
public class DashboardController(ApplicationDbContext db, UserManager<Models.ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Each role receives its own landing page. This also prevents Students and
        // Teachers from retrieving administration-wide counts through /Dashboard.
        if (User.IsInRole("Student")) return RedirectToAction("Index", "Student");
        if (User.IsInRole("Teacher")) return RedirectToAction("Index", "Teacher");
        if (!User.IsInRole("Admin")) return Forbid();

        return View(new DashboardViewModel(
            await db.Students.CountAsync(), await db.Teachers.CountAsync(), await db.Courses.CountAsync(),
            await users.Users.CountAsync(), await db.Courses.Include(x => x.Teacher).OrderByDescending(x => x.Id).Take(5).ToListAsync()));
    }
}
