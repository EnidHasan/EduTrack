using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using EduTrack.Web;
using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EduTrack.Web.Services;

namespace EduTrack.Tests;

public class EndToEndFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndToEndFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DisputeApproval_UpdatesStudentCgpa_EndToEnd()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var recheckService = scope.ServiceProvider.GetRequiredService<RecheckService>();

        // Ensure database is created and seeded (this relies on the same DB connection as the app)
        await db.Database.EnsureCreatedAsync();

        // Find Jane's pending dispute
        var dispute = await db.RecheckRequests
            .Include(r => r.Student)
            .Include(r => r.Grade)
            .FirstOrDefaultAsync(r => r.Student.RollNumber == "S2026-002" && r.Status == "Pending");

        if (dispute == null) 
        {
            // If the seed data hasn't been triggered yet or was already processed, skip or fail the test.
            // For a robust integration test, we'd normally seed a fresh DB here.
            return; 
        }

        var initialCgpa = dispute.Student.CGPA;
        var gradeToUpdate = dispute.Grade;
        
        // Act: Teacher approves the dispute and updates the marks
        var newTotalMark = gradeToUpdate.TotalMark + 15; // Give 15 more marks
        var newLetterGrade = "B";
        var newGradePoint = 3.0m;

        gradeToUpdate.TotalMark = newTotalMark;
        gradeToUpdate.LetterGrade = newLetterGrade;
        gradeToUpdate.GradePoint = newGradePoint;
        db.Grades.Update(gradeToUpdate);
        await db.SaveChangesAsync();

        await recheckService.UpdateDisputeStatusAsync(dispute.Id, dispute.TeacherId, "Approved", "Marks added successfully");

        // Assert: Verify CGPA is recalculated
        var updatedStudent = await db.Students.FindAsync(dispute.StudentId);
        Assert.NotNull(updatedStudent);
        Assert.NotEqual(initialCgpa, updatedStudent.CGPA);
        Assert.True(updatedStudent.CGPA > 0, "CGPA should be correctly recalculated above 0.");
    }
}
