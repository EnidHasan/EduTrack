using EduTrack.Web.Data;
using EduTrack.Web.Models;
using EduTrack.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EduTrack.Tests;

public class RecheckServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task UpdateDisputeStatusAsync_Approved_RecalculatesGradeAndCgpa()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var gradeCalc = new GradeCalculatorService();
        var cgpaCalc = new CgpaCalculationService(db);
        var service = new RecheckService(db, cgpaCalc, gradeCalc);

        var student = new Student { Id = 1, RollNumber = "S101", FullName = "Test Student", Email = "test@student.com", Department = "CSE", CGPA = 0m };
        var teacher = new Teacher { Id = 1, EmployeeId = "T101", FullName = "Test Teacher", Email = "test@teacher.com", Department = "CSE" };
        var course = new Course { Id = 1, CourseCode = "CSE101", CourseName = "Intro CS", CreditHours = 3, Semester = "Spring 2026", TeacherId = 1 };

        db.Students.Add(student);
        db.Teachers.Add(teacher);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var enrollment = new Enrollment { Id = 1, StudentId = 1, CourseId = 1, Semester = "Spring 2026" };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var grade = new Grade
        {
            Id = 1,
            EnrollmentId = 1,
            MidtermMark = 15,
            FinalMark = 35,
            AssignmentMark = 10,
            AttendanceMark = 5,
            TotalMark = 65,
            LetterGrade = "B+",
            GradePoint = 3.25m
        };
        db.Grades.Add(grade);
        await db.SaveChangesAsync();

        var dispute = new RecheckRequest
        {
            Id = 1,
            StudentId = 1,
            TeacherId = 1,
            GradeId = 1,
            Status = "Pending",
            RequestDate = DateTime.UtcNow
        };
        db.RecheckRequests.Add(dispute);
        await db.SaveChangesAsync();

        // Teacher updates marks before approving dispute
        grade.MidtermMark = 20; // +5 marks
        db.Grades.Update(grade);
        await db.SaveChangesAsync();

        // Act
        var (success, message) = await service.UpdateDisputeStatusAsync(1, 1, "Approved", "Approved with updated marks");

        // Assert
        Assert.True(success);
        var updatedDispute = await db.RecheckRequests.FindAsync(1);
        Assert.Equal("Approved", updatedDispute!.Status);

        var updatedGrade = await db.Grades.FindAsync(1);
        Assert.Equal(70m, updatedGrade!.TotalMark);
        Assert.Equal("A-", updatedGrade.LetterGrade);
        Assert.Equal(3.50m, updatedGrade.GradePoint);

        var updatedStudent = await db.Students.FindAsync(1);
        Assert.Equal(3.50m, updatedStudent!.CGPA);
    }
}
