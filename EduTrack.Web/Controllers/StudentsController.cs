using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Controllers;
[Authorize(Roles = "Admin")]
public class StudentsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q) { ViewBag.Query = q; var x = db.Students.AsNoTracking(); if (!string.IsNullOrWhiteSpace(q)) x = x.Where(s => s.FullName.Contains(q) || s.RollNumber.Contains(q) || s.Email.Contains(q)); return View(await x.OrderBy(s => s.FullName).ToListAsync()); }
    public IActionResult Create() => View("Form", new Student());
    public async Task<IActionResult> Edit(int id) => (await db.Students.FindAsync(id)) is { } x ? View("Form", x) : NotFound();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Student model) { if (!ModelState.IsValid) return View("Form", model); if (model.Id == 0) db.Add(model); else db.Update(model); try { await db.SaveChangesAsync(); TempData["Success"] = $"Student {model.FullName} saved."; return RedirectToAction(nameof(Index)); } catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "Roll number or email already exists."); return View("Form", model); } }
    public async Task<IActionResult> Delete(int id) => (await db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)) is { } x ? View(x) : NotFound();
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) { var x = await db.Students.FindAsync(id); if (x is not null) { db.Remove(x); await db.SaveChangesAsync(); TempData["Success"] = "Student deleted."; } return RedirectToAction(nameof(Index)); }
}
