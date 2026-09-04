using EduTrack.Web.Data;
using EduTrack.Web.Models;
using EduTrack.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class StudentsController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(string? q) { ViewBag.Query = q; var x = db.Students.AsNoTracking(); if (!string.IsNullOrWhiteSpace(q)) x = x.Where(s => s.FullName.Contains(q) || s.RollNumber.Contains(q) || s.Email.Contains(q)); return View(await x.OrderBy(s => s.FullName).ToListAsync()); }
    public async Task<IActionResult> Details(int id)
    {
        var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (student is null) return NotFound();

        var enrollments = await db.Enrollments
            .AsNoTracking()
            .Include(x => x.Course)
                .ThenInclude(x => x!.Teacher)
            .Include(x => x.Grade)
            .Where(x => x.StudentId == id)
            .OrderByDescending(x => x.AcademicYear)
            .ThenByDescending(x => x.Semester)
            .ThenBy(x => x.Course!.CourseCode)
            .ToListAsync();

        return View(new StudentDetailsViewModel(student, enrollments));
    }
    public IActionResult Create() => View("Form", new Student());
    public async Task<IActionResult> Edit(int id) => (await db.Students.FindAsync(id)) is { } x ? View("Form", x) : NotFound();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Student model)
    {
        var creating = model.Id == 0;
        if (creating && string.IsNullOrWhiteSpace(model.TemporaryPassword)) ModelState.AddModelError(nameof(model.TemporaryPassword), "Set a temporary password for the student's first login.");
        if (!ModelState.IsValid) return View("Form", model);
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            if (creating)
            {
                if (await users.FindByEmailAsync(model.Email) is not null) { ModelState.AddModelError(nameof(model.Email), "A login account already uses this email."); return View("Form", model); }
                var account = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, ProfileType = "Student", EmailConfirmed = true, IsActive = model.IsActive, MustChangePassword = true };
                var result = await users.CreateAsync(account, model.TemporaryPassword!);
                if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(nameof(model.TemporaryPassword), error.Description); return View("Form", model); }
                await users.AddToRoleAsync(account, "Student");
                model.ApplicationUserId = account.Id; db.Add(model);
            }
            else
            {
                var existing = await db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id); if (existing is null) return NotFound();
                model.ApplicationUserId = existing.ApplicationUserId; db.Update(model);
                if (existing.ApplicationUserId is not null && await users.FindByIdAsync(existing.ApplicationUserId) is { } account)
                {
                    account.FullName = model.FullName; account.Email = account.UserName = model.Email; account.IsActive = model.IsActive;
                    var result = await users.UpdateAsync(account); if (!result.Succeeded) foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                    if (!result.Succeeded) return View("Form", model);
                }
            }
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            TempData["Success"] = creating ? $"Student {model.FullName} created with a login account." : $"Student {model.FullName} updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException) { await transaction.RollbackAsync(); ModelState.AddModelError(string.Empty, "Roll number or email already exists."); return View("Form", model); }
    }
    public async Task<IActionResult> Delete(int id) => (await db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)) is { } x ? View(x) : NotFound();
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) { var x = await db.Students.FindAsync(id); if (x is not null) { var account = x.ApplicationUserId is null ? null : await users.FindByIdAsync(x.ApplicationUserId); db.Remove(x); await db.SaveChangesAsync(); if (account is not null) await users.DeleteAsync(account); TempData["Success"] = "Student and login account deleted."; } return RedirectToAction(nameof(Index)); }
}
