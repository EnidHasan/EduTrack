using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web;

public class SeedSampleDataScript
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Ensure Roles Exist
        string[] roles = ["Admin", "Teacher", "Student"];
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 2. Create Teachers
        var teacher1User = await CreateUserAsync(userManager, "dr.smith@edutrack.edu", "Dr. Robert Smith", "Teacher");
        var teacher2User = await CreateUserAsync(userManager, "prof.sarah@edutrack.edu", "Prof. Sarah Johnson", "Teacher");

        var teacher1 = await GetOrCreateTeacherAsync(db, teacher1User.Id, "T-1001", "Dr. Robert Smith", "dr.smith@edutrack.edu", "Computer Science", "Professor");
        var teacher2 = await GetOrCreateTeacherAsync(db, teacher2User.Id, "T-1002", "Prof. Sarah Johnson", "prof.sarah@edutrack.edu", "Computer Science", "Associate Professor");

        // 3. Create Courses with Exam Schedules
        var course1 = await GetOrCreateCourseAsync(db, "CSE-301", "Database Management Systems", 3.0m, "Fall 2026", teacher1.Id, DateTime.Today.AddDays(5), "09:30 AM - 12:30 PM (Lab 4)");
        var course2 = await GetOrCreateCourseAsync(db, "CSE-305", "Software Engineering & Web Apps", 4.0m, "Fall 2026", teacher2.Id, DateTime.Today.AddDays(12), "02:00 PM - 05:00 PM (Auditorium B)");
        var course3 = await GetOrCreateCourseAsync(db, "CSE-309", "Computer Networks & Security", 3.0m, "Fall 2026", teacher1.Id, DateTime.Today.AddDays(18), "10:00 AM - 01:00 PM (Room 305)");

        // 4. Create Students
        var student1User = await CreateUserAsync(userManager, "alex.student@edutrack.edu", "Alex Turner", "Student");
        var student2User = await CreateUserAsync(userManager, "emma.watson@edutrack.edu", "Emma Watson", "Student");

        var student1 = await GetOrCreateStudentAsync(db, student1User.Id, "S2026-001", "Alex Turner", "alex.student@edutrack.edu", "CSE", 3.75m);
        var student2 = await GetOrCreateStudentAsync(db, student2User.Id, "S2026-002", "Emma Watson", "emma.watson@edutrack.edu", "CSE", 3.90m);

        // 5. Create Enrollments
        await EnsureEnrollmentAsync(db, student1.Id, course1.Id, "Fall 2026", 2026);
        await EnsureEnrollmentAsync(db, student1.Id, course2.Id, "Fall 2026", 2026);
        await EnsureEnrollmentAsync(db, student1.Id, course3.Id, "Fall 2026", 2026);

        await EnsureEnrollmentAsync(db, student2.Id, course1.Id, "Fall 2026", 2026);
        await EnsureEnrollmentAsync(db, student2.Id, course2.Id, "Fall 2026", 2026);
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                ProfileType = role,
                MustChangePassword = false
            };
            var result = await userManager.CreateAsync(user, "Password@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
        return user;
    }

    private static async Task<Teacher> GetOrCreateTeacherAsync(ApplicationDbContext db, string userId, string empId, string name, string email, string dept, string desig)
    {
        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == userId);
        if (teacher == null)
        {
            teacher = new Teacher
            {
                ApplicationUserId = userId,
                EmployeeId = empId,
                FullName = name,
                Email = email,
                Department = dept,
                Designation = desig,
                IsActive = true
            };
            db.Teachers.Add(teacher);
            await db.SaveChangesAsync();
        }
        return teacher;
    }

    private static async Task<Student> GetOrCreateStudentAsync(ApplicationDbContext db, string userId, string roll, string name, string email, string dept, decimal cgpa)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);
        if (student == null)
        {
            student = new Student
            {
                ApplicationUserId = userId,
                RollNumber = roll,
                FullName = name,
                Email = email,
                Department = dept,
                CGPA = cgpa,
                IsActive = true
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }
        return student;
    }

    private static async Task<Course> GetOrCreateCourseAsync(ApplicationDbContext db, string code, string title, decimal credits, string semester, int teacherId, DateTime examDate, string examTime)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.CourseCode == code);
        if (course == null)
        {
            course = new Course
            {
                CourseCode = code,
                CourseName = title,
                CreditHours = credits,
                Semester = semester,
                TeacherId = teacherId,
                IsActive = true,
                FinalExamDate = examDate,
                FinalExamTime = examTime
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
        }
        else
        {
            course.FinalExamDate = examDate;
            course.FinalExamTime = examTime;
            await db.SaveChangesAsync();
        }
        return course;
    }

    private static async Task EnsureEnrollmentAsync(ApplicationDbContext db, int studentId, int courseId, string semester, int year)
    {
        var exists = await db.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        if (!exists)
        {
            db.Enrollments.Add(new Enrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                Semester = semester,
                AcademicYear = year,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }
    }
}
