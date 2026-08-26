using EduTrack.Web.Models;
using EduTrack.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace EduTrack.Web.Controllers;
public class AccountController(SignInManager<ApplicationUser> signInManager) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Dashboard") : View(new LoginViewModel { ReturnUrl = returnUrl });
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Dashboard")!);
        ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Account locked temporarily after repeated attempts." : "Invalid email or password.");
        return View(model);
    }
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await signInManager.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    [AllowAnonymous] public IActionResult AccessDenied() => View();
}
