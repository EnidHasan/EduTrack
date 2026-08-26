using EduTrack.Web.Models;
using EduTrack.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Controllers;
[Authorize(Roles = "Admin")]
public class UsersController(UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var query = users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.FullName.Contains(q) || (x.Email != null && x.Email.Contains(q)));
        var result = new List<UserListItem>();
        foreach (var user in await query.OrderBy(x => x.FullName).ToListAsync()) result.Add(new(user.Id, user.FullName, user.Email ?? "", (await users.GetRolesAsync(user)).FirstOrDefault() ?? "None", user.IsActive, user.CreatedAt));
        ViewBag.Query = q; return View(result);
    }
    public IActionResult Create() => View("Form", new UserFormViewModel());
    public async Task<IActionResult> Edit(string id)
    {
        var user = await users.FindByIdAsync(id); if (user is null) return NotFound();
        return View("Form", new UserFormViewModel { Id = id, FullName = user.FullName, Email = user.Email ?? "", Role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? "Student", IsActive = user.IsActive });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UserFormViewModel model)
    {
        if (model.Id is null && string.IsNullOrWhiteSpace(model.Password)) ModelState.AddModelError(nameof(model.Password), "A temporary password is required.");
        if (!new[] { "Admin", "Teacher", "Student" }.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Select a valid role.");
        if (!ModelState.IsValid) return View("Form", model);
        ApplicationUser user; IdentityResult result;
        if (model.Id is null)
        {
            user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, ProfileType = model.Role, IsActive = model.IsActive, EmailConfirmed = true };
            result = await users.CreateAsync(user, model.Password!);
        }
        else
        {
            var existingUser = await users.FindByIdAsync(model.Id); if (existingUser is null) return NotFound();
            user = existingUser;
            user.FullName = model.FullName; user.Email = user.UserName = model.Email; user.ProfileType = model.Role; user.IsActive = model.IsActive;
            result = await users.UpdateAsync(user);
            if (result.Succeeded && !string.IsNullOrWhiteSpace(model.Password)) { var token = await users.GeneratePasswordResetTokenAsync(user); result = await users.ResetPasswordAsync(user, token, model.Password); }
        }
        if (result.Succeeded)
        {
            var oldRoles = await users.GetRolesAsync(user); if (oldRoles.Count > 0) await users.RemoveFromRolesAsync(user, oldRoles); await users.AddToRoleAsync(user, model.Role);
            TempData["Success"] = $"User {model.FullName} saved."; return RedirectToAction(nameof(Index));
        }
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View("Form", model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string id)
    {
        var user = await users.FindByIdAsync(id); if (user is null) return NotFound();
        if (users.GetUserId(User) == id) { TempData["Error"] = "You cannot disable your own account."; return RedirectToAction(nameof(Index)); }
        user.IsActive = !user.IsActive; user.LockoutEnd = user.IsActive ? null : DateTimeOffset.MaxValue; await users.UpdateAsync(user);
        TempData["Success"] = $"{user.FullName} is now {(user.IsActive ? "active" : "disabled")}."; return RedirectToAction(nameof(Index));
    }
}
