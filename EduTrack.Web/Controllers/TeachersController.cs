using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class TeachersController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(string? q) { ViewBag.Query = q; var x = db.Teachers.AsNoTracking(); if (!string.IsNullOrWhiteSpace(q)) x = x.Where(s => s.FullName.Contains(q) || s.EmployeeId.Contains(q) || s.Email.Contains(q)); return View(await x.OrderBy(s => s.FullName).ToListAsync()); }
    public IActionResult Create() => View("Form", new Teacher());
    public async Task<IActionResult> Edit(int id) => (await db.Teachers.FindAsync(id)) is { } x ? View("Form", x) : NotFound();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Teacher model)
    {
        var creating = model.Id == 0;
        if (creating && string.IsNullOrWhiteSpace(model.TemporaryPassword)) ModelState.AddModelError(nameof(model.TemporaryPassword), "Set a temporary password for the teacher's first login.");
        if (!ModelState.IsValid) return View("Form", model);
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            if (creating)
            {
                if (await users.FindByEmailAsync(model.Email) is not null) { ModelState.AddModelError(nameof(model.Email), "A login account already uses this email."); return View("Form", model); }
                var account = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, ProfileType = "Teacher", EmailConfirmed = true, IsActive = model.IsActive, MustChangePassword = true };
                var result = await users.CreateAsync(account, model.TemporaryPassword!);
                if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(nameof(model.TemporaryPassword), error.Description); return View("Form", model); }
                await users.AddToRoleAsync(account, "Teacher");
                model.ApplicationUserId = account.Id; db.Add(model);
            }
            else
            {
                var existing = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id); if (existing is null) return NotFound();
                model.ApplicationUserId = existing.ApplicationUserId; db.Update(model);
                if (existing.ApplicationUserId is not null && await users.FindByIdAsync(existing.ApplicationUserId) is { } account)
                {
                    account.FullName = model.FullName; account.Email = account.UserName = model.Email; account.IsActive = model.IsActive;
                    var result = await users.UpdateAsync(account); if (!result.Succeeded) foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                    if (!result.Succeeded) return View("Form", model);
                }
            }
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            TempData["Success"] = creating ? $"Teacher {model.FullName} created with a login account." : $"Teacher {model.FullName} updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException) { await transaction.RollbackAsync(); ModelState.AddModelError(string.Empty, "Employee ID or email already exists."); return View("Form", model); }
    }
    public async Task<IActionResult> Delete(int id)
    {
        var teacher = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (teacher is null) return NotFound();

        ViewBag.RecheckRequestCount = await db.RecheckRequests.CountAsync(x => x.TeacherId == id);
        return View(teacher);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var teacher = await db.Teachers.FindAsync(id);
        if (teacher is null) return RedirectToAction(nameof(Index));

        if (await db.RecheckRequests.AnyAsync(x => x.TeacherId == id))
        {
            TempData["Error"] = $"{teacher.FullName} cannot be deleted because the teacher has associated grade recheck history. Disable the login account instead to preserve academic records.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var account = teacher.ApplicationUserId is null
                ? null
                : await users.FindByIdAsync(teacher.ApplicationUserId);

            db.Remove(teacher);
            await db.SaveChangesAsync();

            if (account is not null)
            {
                var accountResult = await users.DeleteAsync(account);
                if (!accountResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "The teacher could not be deleted because the linked login account could not be removed. No records were changed.";
                    return RedirectToAction(nameof(Index));
                }
            }

            await transaction.CommitAsync();
            TempData["Success"] = "Teacher and login account deleted.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = $"{teacher.FullName} cannot be deleted because related academic records still exist. Disable the login account instead.";
        }

        return RedirectToAction(nameof(Index));
    }
}
