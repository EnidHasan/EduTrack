using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Controllers;
[Authorize(Roles = "Admin")]
public class EnrollmentsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Query = q;
        var x = db.Enrollments.Include(e => e.Student).Include(e => e.Course).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
            x = x.Where(e => e.Student!.FullName.Contains(q) || e.Course!.CourseCode.Contains(q) || e.Course!.CourseName.Contains(q));
        return View(await x.OrderByDescending(e => e.EnrolledOn).ToListAsync());
    }

    public async Task<IActionResult> Create() { await Lookups(); return View("Form", new Enrollment()); }
    public async Task<IActionResult> Edit(int id) { var x = await db.Enrollments.FindAsync(id); if (x is null) return NotFound(); await Lookups(); return View("Form", x); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Enrollment model)
    {
        if (!ModelState.IsValid) { await Lookups(); return View("Form", model); }
        if (model.Id == 0) db.Add(model); else db.Update(model);
        try
        {
            await db.SaveChangesAsync();
            TempData["Success"] = "Enrollment saved.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "This student is already enrolled in that course.");
            await Lookups();
            return View("Form", model);
        }
    }

    public async Task<IActionResult> Delete(int id) =>
        (await db.Enrollments.Include(e => e.Student).Include(e => e.Course).AsNoTracking().FirstOrDefaultAsync(e => e.Id == id)) is { } x ? View(x) : NotFound();

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var x = await db.Enrollments.FindAsync(id);
        if (x is not null) { db.Remove(x); await db.SaveChangesAsync(); TempData["Success"] = "Enrollment deleted."; }
        return RedirectToAction(nameof(Index));
    }

    private async Task Lookups()
    {
        ViewBag.StudentId = new SelectList(await db.Students.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync(), "Id", "FullName");
        ViewBag.CourseId = new SelectList(await db.Courses.Where(x => x.IsActive).OrderBy(x => x.CourseCode).ToListAsync(), "Id", "CourseCode");
    }
}
