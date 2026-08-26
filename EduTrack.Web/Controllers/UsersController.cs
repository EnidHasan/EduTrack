using EduTrack.Web.Data;
using EduTrack.Web.Models;
using EduTrack.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController(UserManager<ApplicationUser> users, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        await RepairLegacyAccountLinksAsync();
        var query = users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.FullName.Contains(q) || (x.Email != null && x.Email.Contains(q)));
        var result = new List<UserListItem>();
        var currentId = users.GetUserId(User);
        foreach (var user in await query.OrderBy(x => x.FullName).ToListAsync())
        {
            var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);
            var teacher = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);
            var source = student is not null ? $"Student · {student.RollNumber}" : teacher is not null ? $"Teacher · {teacher.EmployeeId}" : (await users.IsInRoleAsync(user, "Admin") ? "Administration" : "Unlinked legacy account");
            result.Add(new(user.Id, user.FullName, user.Email ?? "", (await users.GetRolesAsync(user)).FirstOrDefault() ?? "None", user.IsActive, user.CreatedAt, source, student is not null || teacher is not null, user.Id == currentId));
        }
        ViewBag.Query = q; return View(result);
    }

    public IActionResult Create() => View("Form", new UserFormViewModel { Role = "Admin", LinkedRecord = "Administration" });

    public async Task<IActionResult> Edit(string id)
    {
        var user = await users.FindByIdAsync(id); if (user is null) return NotFound();
        var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.ApplicationUserId == id);
        var teacher = await db.Teachers.AsNoTracking().FirstOrDefaultAsync(x => x.ApplicationUserId == id);
        var role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? "None";
        return View("Form", new UserFormViewModel { Id = id, FullName = user.FullName, Email = user.Email ?? "", Role = role, IsActive = user.IsActive, IsRoleLocked = true, LinkedRecord = student is not null ? $"Student profile · {student.RollNumber}" : teacher is not null ? $"Teacher profile · {teacher.EmployeeId}" : role == "Admin" ? "Administration" : "Unlinked legacy account" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UserFormViewModel model)
    {
        if (model.Id is null && string.IsNullOrWhiteSpace(model.Password)) ModelState.AddModelError(nameof(model.Password), "A temporary password is required.");
        if (model.Id is null) model.Role = "Admin";
        if (!ModelState.IsValid) return View("Form", model);
        ApplicationUser user; IdentityResult result;
        if (model.Id is null)
        {
            user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, ProfileType = "Admin", IsActive = model.IsActive, EmailConfirmed = true, MustChangePassword = true };
            result = await users.CreateAsync(user, model.Password!);
            if (result.Succeeded) await users.AddToRoleAsync(user, "Admin");
        }
        else
        {
            user = await users.FindByIdAsync(model.Id) ?? throw new InvalidOperationException("Account not found.");
            var student = await db.Students.FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);
            var teacher = await db.Teachers.FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);
            var actualRole = student is not null ? "Student" : teacher is not null ? "Teacher" : (await users.GetRolesAsync(user)).FirstOrDefault() ?? "None";
            model.Role = actualRole; model.IsRoleLocked = true;
            user.FullName = model.FullName; user.Email = user.UserName = model.Email; user.ProfileType = actualRole; user.IsActive = model.IsActive;
            if (student is not null) { student.FullName = model.FullName; student.Email = model.Email; model.LinkedRecord = $"Student profile · {student.RollNumber}"; }
            if (teacher is not null) { teacher.FullName = model.FullName; teacher.Email = model.Email; model.LinkedRecord = $"Teacher profile · {teacher.EmployeeId}"; }
            result = await users.UpdateAsync(user);
            if (result.Succeeded && !string.IsNullOrWhiteSpace(model.Password)) { var token = await users.GeneratePasswordResetTokenAsync(user); result = await users.ResetPasswordAsync(user, token, model.Password); if (result.Succeeded) { user.MustChangePassword = true; await users.UpdateAsync(user); } }
            if (result.Succeeded) await db.SaveChangesAsync();
        }
        if (result.Succeeded) { TempData["Success"] = $"Account for {model.FullName} saved."; return RedirectToAction(nameof(Index)); }
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string id)
    {
        var user = await users.FindByIdAsync(id); if (user is null) return NotFound();
        if (users.GetUserId(User) == id) { TempData["Error"] = "You cannot disable your own account."; return RedirectToAction(nameof(Index)); }
        user.IsActive = !user.IsActive; user.LockoutEnd = user.IsActive ? null : DateTimeOffset.MaxValue; user.AccessFailedCount = 0; await users.UpdateAsync(user);
        TempData["Success"] = $"{user.FullName} is now {(user.IsActive ? "active" : "disabled")}."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await users.FindByIdAsync(id); if (user is null) return NotFound();
        if (users.GetUserId(User) == id) { TempData["Error"] = "You cannot delete your own account."; return RedirectToAction(nameof(Index)); }
        var linked = await db.Students.AnyAsync(x => x.ApplicationUserId == id) || await db.Teachers.AnyAsync(x => x.ApplicationUserId == id);
        if (linked) { TempData["Error"] = "This account is linked to an academic profile. Delete it from Students or Teachers instead."; return RedirectToAction(nameof(Index)); }
        await users.DeleteAsync(user); TempData["Success"] = $"Unlinked account {user.FullName} deleted."; return RedirectToAction(nameof(Index));
    }

    private async Task RepairLegacyAccountLinksAsync()
    {
        var allUsers = await users.Users.ToListAsync();
        foreach (var user in allUsers)
        {
            var student = await db.Students.FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);
            var teacher = await db.Teachers.FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);
            if (student is null && teacher is null && user.Email is not null)
            {
                var studentByEmail = await db.Students.FirstOrDefaultAsync(x => x.ApplicationUserId == null && x.Email == user.Email);
                var teacherByEmail = await db.Teachers.FirstOrDefaultAsync(x => x.ApplicationUserId == null && x.Email == user.Email);
                if (studentByEmail is not null && teacherByEmail is null) { studentByEmail.ApplicationUserId = user.Id; student = studentByEmail; }
                else if (teacherByEmail is not null && studentByEmail is null) { teacherByEmail.ApplicationUserId = user.Id; teacher = teacherByEmail; }
            }
            var requiredRole = student is not null ? "Student" : teacher is not null ? "Teacher" : null;
            if (requiredRole is not null)
            {
                var currentRoles = await users.GetRolesAsync(user);
                if (currentRoles.Count != 1 || currentRoles[0] != requiredRole) { if (currentRoles.Count > 0) await users.RemoveFromRolesAsync(user, currentRoles); await users.AddToRoleAsync(user, requiredRole); }
                user.ProfileType = requiredRole; await users.UpdateAsync(user);
            }
        }
        await db.SaveChangesAsync();
    }
}
