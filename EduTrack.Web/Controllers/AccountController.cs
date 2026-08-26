using EduTrack.Web.Models;
using EduTrack.Web.ViewModels;
using EduTrack.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace EduTrack.Web.Controllers;

public class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> users, ApplicationDbContext db) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Dashboard") : View(new LoginViewModel { ReturnUrl = returnUrl });
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var user = await users.FindByEmailAsync(model.Email);
            if (user?.MustChangePassword == true) return RedirectToAction(nameof(ChangePassword));
            return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Dashboard")!);
        }
        ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Account locked temporarily after repeated attempts. Contact an administrator if you need immediate access." : "Invalid email or password.");
        return View(model);
    }
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await users.GetUserAsync(User); if (user is null) return Challenge();
        return View(new ProfileViewModel { FullName = user.FullName, Email = user.Email ?? "", PhoneNumber = user.PhoneNumber, Role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? "Member" });
    }
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await users.GetUserAsync(User); if (user is null) return Challenge();
        model.Email = user.Email ?? ""; model.Role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? "Member";
        if (!ModelState.IsValid) return View(model);
        user.FullName = model.FullName; user.PhoneNumber = model.PhoneNumber;
        var student = db.Students.FirstOrDefault(x => x.ApplicationUserId == user.Id); if (student is not null) student.FullName = model.FullName;
        var teacher = db.Teachers.FirstOrDefault(x => x.ApplicationUserId == user.Id); if (teacher is not null) teacher.FullName = model.FullName;
        var result = await users.UpdateAsync(user);
        if (result.Succeeded) { await db.SaveChangesAsync(); TempData["Success"] = "Your profile has been updated."; return RedirectToAction(nameof(Profile)); }
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model);
    }
    [Authorize]
    public async Task<IActionResult> ChangePassword()
    {
        var user = await users.GetUserAsync(User); if (user is null) return Challenge();
        return View(new ChangePasswordViewModel { IsFirstLogin = user.MustChangePassword });
    }
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await users.GetUserAsync(User); if (user is null) return Challenge();
        model.IsFirstLogin = user.MustChangePassword;
        if (!ModelState.IsValid) return View(model);
        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model); }
        user.MustChangePassword = false; await users.UpdateAsync(user); await signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Your password has been changed securely.";
        return RedirectToAction(nameof(Profile));
    }
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await signInManager.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    [AllowAnonymous] public IActionResult AccessDenied() => View();
}
