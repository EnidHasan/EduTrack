using EduTrack.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Data;
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IWebHostEnvironment environment)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (environment.IsDevelopment()) await db.Database.MigrateAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Teacher", "Student" }) if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var email = config["SeedAdmin:Email"] ?? "admin@edutrack.edu";
        var password = config["SeedAdmin:Password"] ?? "Admin@12345";
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await users.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser { UserName = email, Email = email, FullName = "System Administrator", EmailConfirmed = true, ProfileType = "Admin", MustChangePassword = false };
            var result = await users.CreateAsync(admin, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            await users.AddToRoleAsync(admin, "Admin");
        }
    }
}
