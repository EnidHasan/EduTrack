using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Controllers;
[Authorize(Roles = "Admin")]
public class CoursesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q) { ViewBag.Query = q; var x = db.Courses.Include(c => c.Teacher).AsNoTracking(); if (!string.IsNullOrWhiteSpace(q)) x = x.Where(c => c.CourseName.Contains(q) || c.CourseCode.Contains(q)); return View(await x.OrderBy(c => c.CourseCode).ToListAsync()); }
    public async Task<IActionResult> Create() { await Teachers(); return View("Form", new Course()); }
    public async Task<IActionResult> Edit(int id) { var x = await db.Courses.FindAsync(id); if (x is null) return NotFound(); await Teachers(); return View("Form", x); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Course model) { if (!ModelState.IsValid) { await Teachers(); return View("Form", model); } if (model.Id == 0) db.Add(model); else db.Update(model); try { await db.SaveChangesAsync(); TempData["Success"] = $"Course {model.CourseCode} saved."; return RedirectToAction(nameof(Index)); } catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "Course code already exists."); await Teachers(); return View("Form", model); } }
    public async Task<IActionResult> Delete(int id) => (await db.Courses.Include(x => x.Teacher).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)) is { } x ? View(x) : NotFound();
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) { var x = await db.Courses.FindAsync(id); if (x is not null) { db.Remove(x); await db.SaveChangesAsync(); TempData["Success"] = "Course deleted."; } return RedirectToAction(nameof(Index)); }
    private async Task Teachers() => ViewBag.TeacherId = new SelectList(await db.Teachers.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync(), "Id", "FullName");
}
