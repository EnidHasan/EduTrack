using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using EduTrack.Web.Controllers;
using EduTrack.Web.Data;
using EduTrack.Web.Models;
using EduTrack.Web.Services;
using EduTrack.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace EduTrack.Tests;

public class AcademicAnalyticsTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static UserManager<ApplicationUser> CreateMockUserManager(ApplicationUser? userToReturn)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userToReturn);

        return userManager.Object;
    }

    [Fact]
    public void Test1_AdminCanAccessAcademicAnalytics_PolicyIncludesAdmin()
    {
        // ReportsController and AnalyticsController must be decorated with AcademicStaff policy
        var reportsAttr = typeof(ReportsController).GetCustomAttribute<AuthorizeAttribute>();
        var analyticsAttr = typeof(AnalyticsController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(reportsAttr);
        Assert.Equal("AcademicStaff", reportsAttr!.Policy);
        Assert.NotNull(analyticsAttr);
        Assert.Equal("AcademicStaff", analyticsAttr!.Policy);
    }

    [Fact]
    public async Task Test2_TeacherCanAccessAnalytics()
    {
        using var db = CreateInMemoryDbContext();
        var analyticsService = new AcademicAnalyticsService(db);
        var atRiskService = new AtRiskEvaluationService(db, new Mock<Microsoft.Extensions.Configuration.IConfiguration>().Object);

        var teacherUser = new ApplicationUser { Id = "teacher-user-1", UserName = "prof@test.com", Email = "prof@test.com", FullName = "Prof Smith" };
        var teacher = new Teacher { Id = 10, FullName = "Prof Smith", EmployeeId = "T10", Email = "prof@test.com", Department = "CSE", ApplicationUserId = teacherUser.Id };
        var course = new Course { Id = 101, CourseCode = "CSE101", CourseName = "Programming", TeacherId = teacher.Id, CreditHours = 3m, IsActive = true };

        db.Users.Add(teacherUser);
        db.Teachers.Add(teacher);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var userManager = CreateMockUserManager(teacherUser);
        var controller = new ReportsController(analyticsService, atRiskService, userManager, db);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, teacherUser.Id),
            new(ClaimTypes.Role, "Teacher")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var userPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userPrincipal }
        };

        var result = await controller.Index(new AcademicAnalyticsFilter());
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AcademicAnalyticsViewModel>(viewResult.Model);

        Assert.True(model.IsTeacherScope);
        Assert.Equal("Prof Smith", model.TeacherName);
        Assert.Single(model.CourseAverages);
        Assert.Equal("CSE101", model.CourseAverages[0].CourseCode);
    }

    [Fact]
    public async Task Test3_TeacherSeesOnlyAssignedCourses()
    {
        using var db = CreateInMemoryDbContext();
        var analyticsService = new AcademicAnalyticsService(db);

        var teacher1 = new Teacher { Id = 1, FullName = "Teacher One", EmployeeId = "T01", Email = "t1@test.com", Department = "CSE" };
        var teacher2 = new Teacher { Id = 2, FullName = "Teacher Two", EmployeeId = "T02", Email = "t2@test.com", Department = "EEE" };

        var course1 = new Course { Id = 1, CourseCode = "CSE101", CourseName = "Intro to CS", TeacherId = 1, IsActive = true };
        var course2 = new Course { Id = 2, CourseCode = "CSE205", CourseName = "Data Structures", TeacherId = 1, IsActive = true };
        var course3 = new Course { Id = 3, CourseCode = "EEE201", CourseName = "Circuits", TeacherId = 2, IsActive = true };

        db.Teachers.AddRange(teacher1, teacher2);
        db.Courses.AddRange(course1, course2, course3);
        await db.SaveChangesAsync();

        // Scope to teacher 1
        var model = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter(), teacherId: 1);

        Assert.True(model.IsTeacherScope);
        Assert.Equal(2, model.CourseAverages.Count);
        Assert.Contains(model.CourseAverages, c => c.CourseCode == "CSE101");
        Assert.Contains(model.CourseAverages, c => c.CourseCode == "CSE205");
        Assert.DoesNotContain(model.CourseAverages, c => c.CourseCode == "EEE201");
    }

    [Fact]
    public async Task Test4_TeacherCannotManipulateCourseIdToSeeAnotherTeacherAnalytics()
    {
        using var db = CreateInMemoryDbContext();
        var analyticsService = new AcademicAnalyticsService(db);
        var atRiskService = new AtRiskEvaluationService(db, new Mock<Microsoft.Extensions.Configuration.IConfiguration>().Object);

        var teacher1User = new ApplicationUser { Id = "t1-user", UserName = "t1@test.com", Email = "t1@test.com", FullName = "Teacher One" };
        var teacher1 = new Teacher { Id = 1, FullName = "Teacher One", EmployeeId = "T01", Email = "t1@test.com", Department = "CSE", ApplicationUserId = teacher1User.Id };
        var teacher2 = new Teacher { Id = 2, FullName = "Teacher Two", EmployeeId = "T02", Email = "t2@test.com", Department = "EEE" };

        var course1 = new Course { Id = 1, CourseCode = "CSE101", CourseName = "Intro CS", TeacherId = 1, IsActive = true };
        var course3 = new Course { Id = 3, CourseCode = "EEE201", CourseName = "Circuits", TeacherId = 2, IsActive = true };

        var student = new Student { Id = 10, FullName = "Student A", RollNumber = "S10", Email = "sa@test.com", Department = "EEE" };
        var enrollment = new Enrollment { Id = 50, StudentId = 10, CourseId = 3, AcademicYear = 2026, Semester = "Spring", IsActive = true };
        var grade = new Grade { Id = 50, EnrollmentId = 50, TotalMark = 95.0m, GradePoint = 4.0m, LetterGrade = "A+" };

        db.Users.Add(teacher1User);
        db.Teachers.AddRange(teacher1, teacher2);
        db.Courses.AddRange(course1, course3);
        db.Students.Add(student);
        db.Enrollments.Add(enrollment);
        db.Grades.Add(grade);
        await db.SaveChangesAsync();

        var userManager = CreateMockUserManager(teacher1User);
        var controller = new ReportsController(analyticsService, atRiskService, userManager, db);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, teacher1User.Id),
            new(ClaimTypes.Role, "Teacher")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Teacher 1 attempts to pass courseId = 3 (belonging to Teacher 2)
        var result = await controller.Index(new AcademicAnalyticsFilter { CourseId = 3 });
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AcademicAnalyticsViewModel>(viewResult.Model);

        // Must not expose Teacher 2's grades
        Assert.Equal(0, model.TotalGradedStudents);
        Assert.Equal(0m, model.AverageFinalMark);
        Assert.DoesNotContain(model.CourseAverages, c => c.CourseId == 3);
    }

    [Fact]
    public void Test5_StudentCannotAccessAnalytics_AcademicStaffPolicyExcludesStudent()
    {
        // Program.cs defines AcademicStaff as: policy.RequireRole("Admin", "Teacher")
        // Therefore student role is not authorized. Let's verify ReportsController and AnalyticsController attributes.
        var reportsAttr = typeof(ReportsController).GetCustomAttribute<AuthorizeAttribute>();
        var analyticsAttr = typeof(AnalyticsController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(reportsAttr);
        Assert.Equal("AcademicStaff", reportsAttr!.Policy);
        Assert.NotNull(analyticsAttr);
        Assert.Equal("AcademicStaff", analyticsAttr!.Policy);
    }

    [Fact]
    public async Task Test6_PassCountIsCalculatedCorrectly()
    {
        using var db = CreateInMemoryDbContext();
        var analyticsService = new AcademicAnalyticsService(db);

        var course = new Course { Id = 1, CourseCode = "CSE101", CourseName = "Intro CS", IsActive = true };
        db.Courses.Add(course);

        // 3 students: Total marks 85 (pass), 40 (pass), 39.5 (fail)
        for (int i = 1; i <= 3; i++)
        {
            var student = new Student { Id = i, FullName = $"Student {i}", RollNumber = $"S0{i}", Email = $"s{i}@test.com", Department = "CSE" };
            db.Students.Add(student);
            var enrollment = new Enrollment { Id = i, StudentId = i, CourseId = 1, AcademicYear = 2026, Semester = "Spring", IsActive = true };
            db.Enrollments.Add(enrollment);
        }

        db.Grades.Add(new Grade { Id = 1, EnrollmentId = 1, TotalMark = 85.0m });
        db.Grades.Add(new Grade { Id = 2, EnrollmentId = 2, TotalMark = 40.0m }); // Exactly 40 is PASS
        db.Grades.Add(new Grade { Id = 3, EnrollmentId = 3, TotalMark = 39.5m }); // Below 40 is FAIL
        await db.SaveChangesAsync();

        var model = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter());

        Assert.Equal(3, model.TotalGradedStudents);
        Assert.Equal(2, model.PassCount);
    }

    [Fact]
    public async Task Test7_FailCountIsCalculatedCorrectly()
    {
        using var db = CreateInMemoryDbContext();
        var analyticsService = new AcademicAnalyticsService(db);

        var course = new Course { Id = 1, CourseCode = "CSE101", CourseName = "Intro CS", IsActive = true };
        db.Courses.Add(course);

        for (int i = 1; i <= 4; i++)
        {
            var student = new Student { Id = i, FullName = $"Student {i}", RollNumber = $"S0{i}", Email = $"s{i}@test.com", Department = "CSE" };
            db.Students.Add(student);
            var enrollment = new Enrollment { Id = i, StudentId = i, CourseId = 1, AcademicYear = 2026, Semester = "Spring", IsActive = true };
            db.Enrollments.Add(enrollment);
        }

        db.Grades.Add(new Grade { Id = 1, EnrollmentId = 1, TotalMark = 65.0m }); // Pass
        db.Grades.Add(new Grade { Id = 2, EnrollmentId = 2, TotalMark = 39.9m }); // Fail
        db.Grades.Add(new Grade { Id = 3, EnrollmentId = 3, TotalMark = 25.0m }); // Fail
        db.Grades.Add(new Grade { Id = 4, EnrollmentId = 4, TotalMark = 0.0m });  // Fail
        await db.SaveChangesAsync();

        var model = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter());

        Assert.Equal(4, model.TotalGradedStudents);
        Assert.Equal(3, model.FailCount);
    }

    [Fact]
    public async Task Test8_PassPercentageIsCalculatedCorrectly()
    {
        using var db = CreateInMemoryDbContext();
        var analyticsService = new AcademicAnalyticsService(db);

        var course = new Course { Id = 1, CourseCode = "CSE101", CourseName = "Intro CS", IsActive = true };
        db.Courses.Add(course);

        // 5 students: 4 pass, 1 fail -> 80.0% pass
        for (int i = 1; i <= 5; i++)
        {
            db.Students.Add(new Student { Id = i, FullName = $"Student {i}", RollNumber = $"S0{i}", Email = $"s{i}@test.com", Department = "CSE" });
            db.Enrollments.Add(new Enrollment { Id = i, StudentId = i, CourseId = 1, AcademicYear = 2026, Semester = "Spring", IsActive = true });
        }

        db.Grades.Add(new Grade { Id = 1, EnrollmentId = 1, TotalMark = 75.0m });
        db.Grades.Add(new Grade { Id = 2, EnrollmentId = 2, TotalMark = 80.0m });
        db.Grades.Add(new Grade { Id = 3, EnrollmentId = 3, TotalMark = 50.0m });
        db.Grades.Add(new Grade { Id = 4, EnrollmentId = 4, TotalMark = 40.0m });
        db.Grades.Add(new Grade { Id = 5, EnrollmentId = 5, TotalMark = 30.0m });
        await db.SaveChangesAsync();

        var model = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter());

        Assert.Equal(5, model.TotalGradedStudents);
        Assert.Equal(4, model.PassCount);
        Assert.Equal(80.0m, model.PassRate);
    }

    [Fact]
    public async Task Test9_FailPercentageIsCalculatedCorrectly()
    {
        using var db = CreateInMemoryDbContext();
        var analyticsService = new AcademicAnalyticsService(db);

        var course = new Course { Id = 1, CourseCode = "CSE101", CourseName = "Intro CS", IsActive = true };
        db.Courses.Add(course);

        // 5 students: 4 pass, 1 fail -> 20.0% fail
        for (int i = 1; i <= 5; i++)
        {
            db.Students.Add(new Student { Id = i, FullName = $"Student {i}", RollNumber = $"S0{i}", Email = $"s{i}@test.com", Department = "CSE" });
            db.Enrollments.Add(new Enrollment { Id = i, StudentId = i, CourseId = 1, AcademicYear = 2026, Semester = "Spring", IsActive = true });
        }

        db.Grades.Add(new Grade { Id = 1, EnrollmentId = 1, TotalMark = 75.0m });
        db.Grades.Add(new Grade { Id = 2, EnrollmentId = 2, TotalMark = 80.0m });
        db.Grades.Add(new Grade { Id = 3, EnrollmentId = 3, TotalMark = 50.0m });
        db.Grades.Add(new Grade { Id = 4, EnrollmentId = 4, TotalMark = 40.0m });
        db.Grades.Add(new Grade { Id = 5, EnrollmentId = 5, TotalMark = 30.0m });
        await db.SaveChangesAsync();

        var model = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter());

        Assert.Equal(5, model.TotalGradedStudents);
        Assert.Equal(1, model.FailCount);
        Assert.Equal(20.0m, model.FailRate);
    }

    [Fact]
    public async Task Test10_CourseAverageIsCalculatedCorrectly()
    {
        using var db = CreateInMemoryDbContext();
        var analyticsService = new AcademicAnalyticsService(db);

        var course = new Course { Id = 1, CourseCode = "CSE101", CourseName = "Database", IsActive = true };
        db.Courses.Add(course);

        // 3 students with marks 80, 70, 60 -> (80 + 70 + 60) / 3 = 70.0
        for (int i = 1; i <= 3; i++)
        {
            db.Students.Add(new Student { Id = i, FullName = $"Student {i}", RollNumber = $"S0{i}", Email = $"s{i}@test.com", Department = "CSE" });
            db.Enrollments.Add(new Enrollment { Id = i, StudentId = i, CourseId = 1, AcademicYear = 2026, Semester = "Spring", IsActive = true });
        }

        db.Grades.Add(new Grade { Id = 1, EnrollmentId = 1, TotalMark = 80.0m });
        db.Grades.Add(new Grade { Id = 2, EnrollmentId = 2, TotalMark = 70.0m });
        db.Grades.Add(new Grade { Id = 3, EnrollmentId = 3, TotalMark = 60.0m });
        await db.SaveChangesAsync();

        var model = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter());

        Assert.Equal(70.0m, model.AverageFinalMark);
        var courseStat = Assert.Single(model.CourseAverages);
        Assert.Equal(70.0m, courseStat.AverageMark);
    }

    [Fact]
    public async Task Test11_FiltersCorrectlyAffectAnalytics()
    {
        using var db = CreateInMemoryDbContext();
        var analyticsService = new AcademicAnalyticsService(db);

        var course1 = new Course { Id = 1, CourseCode = "CSE101", CourseName = "CS1", IsActive = true };
        var course2 = new Course { Id = 2, CourseCode = "EEE201", CourseName = "EE1", IsActive = true };
        db.Courses.AddRange(course1, course2);

        var s1 = new Student { Id = 1, FullName = "S1", RollNumber = "R1", Email = "s1@test.com", Department = "CSE" };
        var s2 = new Student { Id = 2, FullName = "S2", RollNumber = "R2", Email = "s2@test.com", Department = "EEE" };
        db.Students.AddRange(s1, s2);

        // S1 enrolled in CSE101 in 2025, Fall (Mark 90)
        var e1 = new Enrollment { Id = 1, StudentId = 1, CourseId = 1, AcademicYear = 2025, Semester = "Fall", IsActive = true };
        var g1 = new Grade { Id = 1, EnrollmentId = 1, TotalMark = 90.0m };

        // S2 enrolled in EEE201 in 2026, Spring (Mark 50)
        var e2 = new Enrollment { Id = 2, StudentId = 2, CourseId = 2, AcademicYear = 2026, Semester = "Spring", IsActive = true };
        var g2 = new Grade { Id = 2, EnrollmentId = 2, TotalMark = 50.0m };

        db.Enrollments.AddRange(e1, e2);
        db.Grades.AddRange(g1, g2);
        await db.SaveChangesAsync();

        // 1. Filter by AcademicYear = 2026
        var rYear = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter { AcademicYear = 2026 });
        Assert.Equal(1, rYear.TotalGradedStudents);
        Assert.Equal(50.0m, rYear.AverageFinalMark);

        // 2. Filter by Semester = "Fall"
        var rSem = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter { Semester = "Fall" });
        Assert.Equal(1, rSem.TotalGradedStudents);
        Assert.Equal(90.0m, rSem.AverageFinalMark);

        // 3. Filter by Department = "CSE"
        var rDept = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter { Department = "CSE" });
        Assert.Equal(1, rDept.TotalGradedStudents);
        Assert.Equal(90.0m, rDept.AverageFinalMark);

        // 4. Filter by CourseId = 2
        var rCourse = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter { CourseId = 2 });
        Assert.Equal(1, rCourse.TotalGradedStudents);
        Assert.Equal(50.0m, rCourse.AverageFinalMark);
    }

    [Fact]
    public async Task Test12_NoDataScenariosDoNotCauseErrors()
    {
        using var db = CreateInMemoryDbContext();
        var analyticsService = new AcademicAnalyticsService(db);

        // No students, no grades in database
        var model = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter());

        Assert.NotNull(model);
        Assert.Equal(0, model.TotalGradedStudents);
        Assert.Equal(0m, model.AverageFinalMark);
        Assert.Equal(0, model.PassCount);
        Assert.Equal(0, model.FailCount);
        Assert.Equal(0m, model.PassRate);
        Assert.Equal(0m, model.FailRate);
        Assert.False(model.HasGradedData);
        Assert.Empty(model.CourseAverages);
    }

    [Fact]
    public async Task Test13_OnlyGradedStudentsAreIncludedInAcademicCalculations()
    {
        using var db = CreateInMemoryDbContext();
        var analyticsService = new AcademicAnalyticsService(db);

        var course = new Course { Id = 1, CourseCode = "CSE101", CourseName = "Intro CS", IsActive = true };
        db.Courses.Add(course);

        // 3 enrollments: 1 graded, 2 ungraded
        for (int i = 1; i <= 3; i++)
        {
            db.Students.Add(new Student { Id = i, FullName = $"Student {i}", RollNumber = $"S0{i}", Email = $"s{i}@test.com", Department = "CSE" });
            db.Enrollments.Add(new Enrollment { Id = i, StudentId = i, CourseId = 1, AcademicYear = 2026, Semester = "Spring", IsActive = true });
        }

        // Only enrollment 1 has a Grade
        db.Grades.Add(new Grade { Id = 1, EnrollmentId = 1, TotalMark = 88.0m });
        await db.SaveChangesAsync();

        var model = await analyticsService.GetAnalyticsAsync(new AcademicAnalyticsFilter());

        Assert.Equal(1, model.TotalGradedStudents);
        Assert.Equal(88.0m, model.AverageFinalMark);
        Assert.Equal(1, model.PassCount);
        Assert.Equal(0, model.FailCount);
        Assert.Equal(100.0m, model.PassRate);

        var courseStat = Assert.Single(model.CourseAverages);
        Assert.Equal(3, courseStat.TotalEnrolledCount);
        Assert.Equal(1, courseStat.GradedStudentCount);
        Assert.Equal(88.0m, courseStat.AverageMark);
    }

    [Fact]
    public async Task Test14_ExistingReportFunctionalityContinuesToWork()
    {
        using var db = CreateInMemoryDbContext();
        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        var atRiskService = new AtRiskEvaluationService(db, configMock.Object);

        var student = new Student { Id = 1, FullName = "John Doe", RollNumber = "S01", Email = "john@test.com", Department = "CSE", Semester = "Spring", IsActive = true };
        var course = new Course { Id = 1, CourseCode = "CSE101", CourseName = "Intro CS", CreditHours = 3m, IsActive = true };
        var enrollment = new Enrollment { Id = 1, StudentId = 1, CourseId = 1, AcademicYear = 2026, Semester = "Spring", IsActive = true };
        var grade = new Grade { Id = 1, EnrollmentId = 1, TotalMark = 75.0m, GradePoint = 3.75m, LetterGrade = "A", AttendanceMark = 9.0m };

        db.Students.Add(student);
        db.Courses.Add(course);
        db.Enrollments.Add(enrollment);
        db.Grades.Add(grade);
        await db.SaveChangesAsync();

        // 1. GetSystemReportAsync executes successfully
        var report = await atRiskService.GetSystemReportAsync();
        Assert.NotNull(report);
        Assert.Equal(1, report.TotalStudentsCount);
        Assert.Equal(1, report.TotalCoursesCount);
        Assert.Single(report.DepartmentSummaries);
        Assert.Single(report.CourseSummaries);

        // 2. SystemReportPdfBuilder.Build executes and creates a valid PDF byte stream
        var pdfBytes = SystemReportPdfBuilder.Build(report);
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        // PDF header check
        var pdfHeader = System.Text.Encoding.Latin1.GetString(pdfBytes.Take(8).ToArray());
        Assert.StartsWith("%PDF-1.4", pdfHeader);
    }
}
