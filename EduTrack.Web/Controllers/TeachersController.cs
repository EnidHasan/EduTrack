using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Controllers;
[Authorize(Roles = "Admin")]
public class TeachersController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q) { ViewBag.Query = q; var x = db.Teachers.AsNoTracking(); if (!string.IsNullOrWhiteSpace(q)) x = x.Where(s => s.FullName.Contains(q) || s.EmployeeId.Contains(q) || s.Email.Contains(q)); return View(await x.OrderBy(s => s.FullName).ToListAsync()); }
    public IActionResult Create() => View("Form", new Teacher());
    public async Task<IActionResult> Edit(int id) => (await db.Teachers.FindAsync(id)) is { } x ? View("Form", x) : NotFound();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Teacher model) { if (!ModelState.IsValid) return View("Form", model); if (model.Id == 0) db.Add(model); else db.Update(model); try { await db.SaveChangesAsync(); TempData["Success"] = $"Teacher {model.FullName} saved."; return RedirectToAction(nameof(Index)); } catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "Employee ID or email already exists."); return View("Form", model); } }
    public async Task<IActionResult> Delete(int id) => (await db.Teachers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)) is { } x ? View(x) : NotFound();
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) { var x = await db.Teachers.FindAsync(id); if (x is not null) { db.Remove(x); await db.SaveChangesAsync(); TempData["Success"] = "Teacher deleted."; } return RedirectToAction(nameof(Index)); }
}
