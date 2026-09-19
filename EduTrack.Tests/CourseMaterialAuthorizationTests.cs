using System.Threading.Tasks;
using Xunit;
using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EduTrack.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Microsoft.Extensions.Logging;

namespace EduTrack.Tests;

public class CourseMaterialAuthorizationTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private CourseMaterialService GetService(ApplicationDbContext db)
    {
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(System.IO.Path.GetTempPath());
        
        var loggerMock = new Mock<ILogger<CourseMaterialService>>();

        return new CourseMaterialService(db, envMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task CanTeacherManageCourseAsync_AssignedTeacher_ReturnsTrue()
    {
        using var db = GetInMemoryDbContext();
        var service = GetService(db);
        
        db.Teachers.Add(new Teacher { Id = 1, FullName = "Teacher 1", EmployeeId = "T01", Email = "t1@test.com", Department = "CS" });
        db.Courses.Add(new Course { Id = 1, TeacherId = 1, CourseCode = "CS101", CourseName = "Intro" });
        await db.SaveChangesAsync();

        var result = await service.CanTeacherManageCourseAsync(1, 1);
        Assert.True(result);
    }

    [Fact]
    public async Task CanTeacherManageCourseAsync_UnassignedTeacher_ReturnsFalse()
    {
        using var db = GetInMemoryDbContext();
        var service = GetService(db);
        
        db.Teachers.Add(new Teacher { Id = 1, FullName = "Teacher 1", EmployeeId = "T01", Email = "t1@test.com", Department = "CS" });
        db.Teachers.Add(new Teacher { Id = 2, FullName = "Teacher 2", EmployeeId = "T02", Email = "t2@test.com", Department = "CS" });
        db.Courses.Add(new Course { Id = 1, TeacherId = 1, CourseCode = "CS101", CourseName = "Intro" });
        await db.SaveChangesAsync();

        var result = await service.CanTeacherManageCourseAsync(2, 1);
        Assert.False(result);
    }

    [Fact]
    public async Task CanStudentAccessCourseAsync_EnrolledStudent_ReturnsTrue()
    {
        using var db = GetInMemoryDbContext();
        var service = GetService(db);

        db.Students.Add(new Student { Id = 1, FullName = "Student 1", RollNumber = "S01", Email = "s1@test.com", Department = "CS", EnrollmentYear = 2026 });
        db.Courses.Add(new Course { Id = 1, CourseCode = "CS101", CourseName = "Intro" });
        db.Enrollments.Add(new Enrollment { StudentId = 1, CourseId = 1, IsActive = true });
        await db.SaveChangesAsync();

        var result = await service.CanStudentAccessCourseAsync(1, 1);
        Assert.True(result);
    }

    [Fact]
    public async Task CanStudentAccessCourseAsync_UnenrolledStudent_ReturnsFalse()
    {
        using var db = GetInMemoryDbContext();
        var service = GetService(db);

        db.Students.Add(new Student { Id = 1, FullName = "Student 1", RollNumber = "S01", Email = "s1@test.com", Department = "CS", EnrollmentYear = 2026 });
        db.Students.Add(new Student { Id = 2, FullName = "Student 2", RollNumber = "S02", Email = "s2@test.com", Department = "CS", EnrollmentYear = 2026 });
        db.Courses.Add(new Course { Id = 1, CourseCode = "CS101", CourseName = "Intro" });
        db.Enrollments.Add(new Enrollment { StudentId = 1, CourseId = 1, IsActive = true });
        await db.SaveChangesAsync();

        var result = await service.CanStudentAccessCourseAsync(2, 1);
        Assert.False(result);
    }
}
