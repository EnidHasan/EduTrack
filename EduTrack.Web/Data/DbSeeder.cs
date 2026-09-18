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
            new Teacher { EmployeeId = "T-001", FullName = "Dr. Syed Akhter Hossain", Email = "syed.akhter@edutrack.edu", Department = "CSE", Designation = "Professor" },
            new Teacher { EmployeeId = "T-002", FullName = "Dr. Mohammad Ali", Email = "m.ali@edutrack.edu", Department = "EEE", Designation = "Associate Professor" },
            new Teacher { EmployeeId = "T-003", FullName = "Dr. Farhana Zulkernine", Email = "farhana@edutrack.edu", Department = "CSE", Designation = "Assistant Professor" },
            new Teacher { EmployeeId = "T-004", FullName = "Prof. Jamal Uddin", Email = "jamal.uddin@edutrack.edu", Department = "BBA", Designation = "Professor" },
            new Teacher { EmployeeId = "T-005", FullName = "Dr. Anika Tabassum", Email = "anika@edutrack.edu", Department = "Physics", Designation = "Lecturer" },
            new Teacher { EmployeeId = "T-006", FullName = "Dr. Rafiqul Islam", Email = "rafiqul@edutrack.edu", Department = "Mathematics", Designation = "Associate Professor" },
            new Teacher { EmployeeId = "T-007", FullName = "Tariqul Islam", Email = "tariqul@edutrack.edu", Department = "English", Designation = "Lecturer" }
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
                new Course { CourseCode = "CSE101", CourseName = "Intro to Programming", CreditHours = 3, Semester = "Fall 2026", TeacherId = dbTeachers.FirstOrDefault(x => x.Department == "CSE")?.Id },
                new Course { CourseCode = "PHY101", CourseName = "General Physics", CreditHours = 4, Semester = "Fall 2026", TeacherId = dbTeachers.FirstOrDefault(x => x.Department == "Physics")?.Id },
                new Course { CourseCode = "CSE201", CourseName = "Data Structures", CreditHours = 3, Semester = "Fall 2026", TeacherId = dbTeachers.FirstOrDefault(x => x.Department == "CSE")?.Id },
                new Course { CourseCode = "ENG101", CourseName = "Basic English", CreditHours = 3, Semester = "Fall 2026", TeacherId = dbTeachers.FirstOrDefault(x => x.Department == "English")?.Id }
            };

            db.Courses.AddRange(courses);
            await db.SaveChangesAsync();
            dbCourses = courses.ToList();
        }

        // 4. Seed Students
        var studentsToSeed = new[]
        {
            new Student { FullName = "Rakibul Hasan", RollNumber = "S2026-001", Email = "rakib@edutrack.edu", Department = "CSE", EnrollmentYear = 2026, Semester = "Fall 2026" },
            new Student { FullName = "Nusrat Jahan", RollNumber = "S2026-002", Email = "nusrat@edutrack.edu", Department = "CSE", EnrollmentYear = 2026, Semester = "Fall 2026" },
            new Student { FullName = "Mehdi Hasan", RollNumber = "S2026-003", Email = "mehdi@edutrack.edu", Department = "BBA", EnrollmentYear = 2026, Semester = "Fall 2026" },
            new Student { FullName = "Sadia Afrin", RollNumber = "S2026-004", Email = "sadia@edutrack.edu", Department = "EEE", EnrollmentYear = 2026, Semester = "Fall 2026" }
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
            var rakib = dbStudents.FirstOrDefault(x => x.RollNumber == "S2026-001");
            var nusrat = dbStudents.FirstOrDefault(x => x.RollNumber == "S2026-002");
            var cse101 = dbCourses.FirstOrDefault(x => x.CourseCode == "CSE101");
            var phy101 = dbCourses.FirstOrDefault(x => x.CourseCode == "PHY101");
            var eng101 = dbCourses.FirstOrDefault(x => x.CourseCode == "ENG101");

            if (rakib != null && cse101 != null && phy101 != null && eng101 != null)
            {
                var e1 = new Enrollment { StudentId = rakib.Id, CourseId = cse101.Id, Semester = "Fall 2026" };
                var e2 = new Enrollment { StudentId = rakib.Id, CourseId = phy101.Id, Semester = "Fall 2026" };
                var e3 = new Enrollment { StudentId = rakib.Id, CourseId = eng101.Id, Semester = "Fall 2026" };
                db.Enrollments.AddRange(e1, e2, e3);
                await db.SaveChangesAsync();

                db.Grades.AddRange(
                    new Grade { EnrollmentId = e1.Id, AssignmentMark = 18, AttendanceMark = 10, MidtermMark = 16, FinalMark = 40, TotalMark = 84, LetterGrade = "A", GradePoint = 4.0m },
                    new Grade { EnrollmentId = e2.Id, AssignmentMark = 15, AttendanceMark = 9, MidtermMark = 12, FinalMark = 35, TotalMark = 71, LetterGrade = "B", GradePoint = 3.0m },
                    new Grade { EnrollmentId = e3.Id, AssignmentMark = 16, AttendanceMark = 8, MidtermMark = 14, FinalMark = 38, TotalMark = 76, LetterGrade = "A-", GradePoint = 3.5m }
                );
            }

            if (nusrat != null && cse101 != null)
            {
                var e4 = new Enrollment { StudentId = nusrat.Id, CourseId = cse101.Id, Semester = "Fall 2026" };
                db.Enrollments.Add(e4);
                await db.SaveChangesAsync();

                var grade4 = new Grade { EnrollmentId = e4.Id, AssignmentMark = 12, AttendanceMark = 8, MidtermMark = 10, FinalMark = 30, TotalMark = 60, LetterGrade = "C", GradePoint = 2.0m };
                db.Grades.Add(grade4);
                await db.SaveChangesAsync();

                // 6. Seed a sample RecheckRequest for Nusrat's CSE101 grade
                db.RecheckRequests.Add(new RecheckRequest
                {
                    StudentId = nusrat.Id,
                    TeacherId = cse101.TeacherId ?? 1,
                    GradeId = grade4.Id,
                    Status = "Pending",
                    RequestDate = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }
    }
}
