using EduTrack.Web.Models;
using EduTrack.Web.ViewModels;
using EduTrack.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace EduTrack.Web.Controllers;

[Authorize]
public class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> users, ApplicationDbContext db, IWebHostEnvironment env) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null, string? reason = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        if (reason == "disabled") ModelState.AddModelError(string.Empty, "Your account is disabled. Contact an administrator for access.");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var account = await users.FindByEmailAsync(model.Email);
        if (account is not null && !account.IsActive) { ModelState.AddModelError(string.Empty, "This account is disabled. Contact an administrator."); return View(model); }
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
    public async Task<IActionResult> Profile()
    {
        var user = await users.GetUserAsync(User); if (user is null) return Challenge();
        return View(new ProfileViewModel { FullName = user.FullName, Email = user.Email ?? "", PhoneNumber = user.PhoneNumber, Role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? "Member", ProfilePicture = user.ProfilePicture });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await users.GetUserAsync(User); if (user is null) return Challenge();
        model.Email = user.Email ?? ""; model.Role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? "Member";
        model.ProfilePicture = user.ProfilePicture;
        if (!ModelState.IsValid) return View(model);
        
        if (model.ProfileImage != null)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("ProfileImage", "Only JPG, JPEG, PNG, and WEBP images are allowed.");
                return View(model);
            }
            if (model.ProfileImage.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("ProfileImage", "Profile picture cannot exceed 2MB.");
                return View(model);
            }
            
            using var stream = model.ProfileImage.OpenReadStream();
            var header = new byte[12];
            await stream.ReadExactlyAsync(header, 0, 12);
            bool isValid = false;
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) isValid = true; // JPEG
            else if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A) isValid = true; // PNG
            else if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) isValid = true; // WEBP
            
            if (!isValid)
            {
                ModelState.AddModelError("ProfileImage", "Invalid image file content.");
                return View(model);
            }
            
            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldPath = Path.Combine(env.WebRootPath, "images", "profiles", user.ProfilePicture);
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
            
            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(env.WebRootPath, "images", "profiles");
            Directory.CreateDirectory(uploadsFolder);
            var filePath = Path.Combine(uploadsFolder, fileName);
            
            stream.Position = 0;
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }
            user.ProfilePicture = fileName;
        }

        user.FullName = model.FullName; user.PhoneNumber = model.PhoneNumber;
        var student = db.Students.FirstOrDefault(x => x.ApplicationUserId == user.Id); if (student is not null) student.FullName = model.FullName;
        var teacher = db.Teachers.FirstOrDefault(x => x.ApplicationUserId == user.Id); if (teacher is not null) teacher.FullName = model.FullName;
        var result = await users.UpdateAsync(user);
        if (result.Succeeded) { await db.SaveChangesAsync(); TempData["Success"] = "Your profile has been updated."; return RedirectToAction(nameof(Profile)); }
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model);
    }
    public async Task<IActionResult> ChangePassword()
    {
        var user = await users.GetUserAsync(User); if (user is null) return Challenge();
        return View(new ChangePasswordViewModel { IsFirstLogin = user.MustChangePassword });
    }
    [HttpPost, ValidateAntiForgeryToken]
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
