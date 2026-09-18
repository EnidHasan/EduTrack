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
        var dbCourses = await db.Courses.ToListAsync();
        if (!dbCourses.Any())
        {
            var courses = new[]
            {
                new Course { CourseCode = "CS101", CourseName = "Intro to Programming", CreditHours = 3, Semester = "Fall 2026", TeacherId = dbTeachers.FirstOrDefault(x => x.Department == "Computer Science")?.Id },
                new Course { CourseCode = "PHY101", CourseName = "General Physics", CreditHours = 4, Semester = "Fall 2026", TeacherId = dbTeachers.FirstOrDefault(x => x.Department == "Physics")?.Id },
                new Course { CourseCode = "CS201", CourseName = "Data Structures", CreditHours = 3, Semester = "Fall 2026", TeacherId = dbTeachers.FirstOrDefault(x => x.Department == "Computer Science")?.Id }
            };

            db.Courses.AddRange(courses);
            await db.SaveChangesAsync();
            dbCourses = courses.ToList();
        }

        // 4. Seed Students
        var studentsToSeed = new[]
        {
            new Student { FullName = "John Doe", RollNumber = "S2026-001", Email = "john@edutrack.edu", Department = "Computer Science", EnrollmentYear = 2026, Semester = "Fall 2026" },
            new Student { FullName = "Jane Smith", RollNumber = "S2026-002", Email = "jane@edutrack.edu", Department = "Physics", EnrollmentYear = 2026, Semester = "Fall 2026" }
        };

        var dbStudents = new List<Student>();

        foreach (var s in studentsToSeed)
        {
            var existingStudent = await db.Students.FirstOrDefaultAsync(x => x.Email == s.Email);
            if (existingStudent == null)
            {
                var user = new ApplicationUser 
                { 
                    UserName = s.Email, Email = s.Email, FullName = s.FullName, 
                    EmailConfirmed = true, ProfileType = "Student", MustChangePassword = false 
                };
                await userManager.CreateAsync(user, "Student@123");
                await userManager.AddToRoleAsync(user, "Student");
                
                s.ApplicationUserId = user.Id;
                db.Students.Add(s);
                await db.SaveChangesAsync();
                dbStudents.Add(s);
            }
            else
            {
                dbStudents.Add(existingStudent);
            }
        }

        // 5. Seed Enrollments & Grades
        if (!await db.Enrollments.AnyAsync())
        {
            var john = dbStudents.FirstOrDefault(x => x.RollNumber == "S2026-001");
            var jane = dbStudents.FirstOrDefault(x => x.RollNumber == "S2026-002");
            var cs101 = dbCourses.FirstOrDefault(x => x.CourseCode == "CS101");
            var phy101 = dbCourses.FirstOrDefault(x => x.CourseCode == "PHY101");

            if (john != null && cs101 != null && phy101 != null)
            {
                var e1 = new Enrollment { StudentId = john.Id, CourseId = cs101.Id, Semester = "Fall 2026" };
                var e2 = new Enrollment { StudentId = john.Id, CourseId = phy101.Id, Semester = "Fall 2026" };
                db.Enrollments.AddRange(e1, e2);
                await db.SaveChangesAsync();

                db.Grades.AddRange(
                    new Grade { EnrollmentId = e1.Id, AssignmentMark = 18, AttendanceMark = 10, MidtermMark = 16, FinalMark = 40, TotalMark = 84, LetterGrade = "A", GradePoint = 4.0m },
                    new Grade { EnrollmentId = e2.Id, AssignmentMark = 15, AttendanceMark = 9, MidtermMark = 12, FinalMark = 35, TotalMark = 71, LetterGrade = "B", GradePoint = 3.0m }
                );
            }

            if (jane != null && cs101 != null)
            {
                var e3 = new Enrollment { StudentId = jane.Id, CourseId = cs101.Id, Semester = "Fall 2026" };
                db.Enrollments.Add(e3);
                await db.SaveChangesAsync();

                var grade3 = new Grade { EnrollmentId = e3.Id, AssignmentMark = 12, AttendanceMark = 8, MidtermMark = 10, FinalMark = 30, TotalMark = 60, LetterGrade = "C", GradePoint = 2.0m };
                db.Grades.Add(grade3);
                await db.SaveChangesAsync();

                // 6. Seed a sample RecheckRequest for Jane's CS101 grade
                db.RecheckRequests.Add(new RecheckRequest
                {
                    StudentId = jane.Id,
                    TeacherId = cs101.TeacherId ?? 1,
                    GradeId = grade3.Id,
                    Status = "Pending",
                    RequestDate = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }
    }
}
