using EduTrack.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web.Data;

public static class DbSeeder
{
    public static async Task SeedDataAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        // 1. Seed Admin
        var adminEmail = "admin@edutrack.edu";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser 
            { 
                UserName = adminEmail, 
                Email = adminEmail, 
                FullName = "System Administrator", 
                EmailConfirmed = true, 
                ProfileType = "Admin", 
                MustChangePassword = false 
            };
            await userManager.CreateAsync(admin, "Admin@12345");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // 2. Seed Teachers
        var teachersToSeed = new[]
        {
            new Teacher { EmployeeId = "T-001", FullName = "Dr. Alan Turing", Email = "alan@edutrack.edu", Department = "Computer Science", Designation = "Professor" },
            new Teacher { EmployeeId = "T-002", FullName = "Dr. Marie Curie", Email = "marie@edutrack.edu", Department = "Physics", Designation = "Associate Professor" }
        };

        var dbTeachers = new List<Teacher>();

        foreach (var t in teachersToSeed)
        {
            var existingTeacher = await db.Teachers.FirstOrDefaultAsync(x => x.Email == t.Email);
            if (existingTeacher == null)
            {
                var user = new ApplicationUser 
                { 
                    UserName = t.Email, 
                    Email = t.Email, 
                    FullName = t.FullName, 
                    EmailConfirmed = true, 
                    ProfileType = "Teacher", 
                    MustChangePassword = false 
                };
                await userManager.CreateAsync(user, "Teacher@123");
                await userManager.AddToRoleAsync(user, "Teacher");
                
                t.ApplicationUserId = user.Id;
                db.Teachers.Add(t);
                await db.SaveChangesAsync();
                dbTeachers.Add(t);
            }
            else
            {
                dbTeachers.Add(existingTeacher);
            }
        }

        // 3. Seed Courses
        if (!await db.Courses.AnyAsync())
        {
            var courses = new[]
            {
                new Course { CourseCode = "CS101", CourseName = "Intro to Programming", CreditHours = 3, Semester = "Fall 2026", TeacherId = dbTeachers.FirstOrDefault(x => x.Department == "Computer Science")?.Id },
                new Course { CourseCode = "PHY101", CourseName = "General Physics", CreditHours = 4, Semester = "Fall 2026", TeacherId = dbTeachers.FirstOrDefault(x => x.Department == "Physics")?.Id },
                new Course { CourseCode = "CS201", CourseName = "Data Structures", CreditHours = 3, Semester = "Fall 2026", TeacherId = dbTeachers.FirstOrDefault(x => x.Department == "Computer Science")?.Id }
            };

            db.Courses.AddRange(courses);
            await db.SaveChangesAsync();
        }
    }
}
