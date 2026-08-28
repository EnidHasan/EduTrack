using EduTrack.Web.Data;
using EduTrack.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web.Services;

public class RecheckService(ApplicationDbContext db)
{
    public async Task<List<RecheckRequest>> GetStudentRequestsAsync(int studentId)
    {
        return await db.RecheckRequests
            .Include(r => r.Grade!.Enrollment!.Course)
            .Include(r => r.Teacher)
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync();
    }

    public async Task<List<RecheckRequest>> GetTeacherRequestsAsync(int teacherId)
    {
        return await db.RecheckRequests
            .Include(r => r.Grade!.Enrollment!.Course)
            .Include(r => r.Student)
            .Where(r => r.TeacherId == teacherId)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync();
    }

    public async Task<(bool success, string message)> SubmitDisputeAsync(int studentId, int gradeId)
    {
        var grade = await db.Grades
            .Include(g => g.Enrollment!.Course)
            .FirstOrDefaultAsync(g => g.Id == gradeId && g.Enrollment!.StudentId == studentId);

        if (grade == null) return (false, "Grade not found.");

        var existing = await db.RecheckRequests
            .AnyAsync(r => r.GradeId == gradeId && r.Status == "Pending");

        if (existing) return (false, "A pending dispute already exists for this grade.");

        var request = new RecheckRequest
        {
            StudentId = studentId,
            TeacherId = grade.Enrollment!.Course!.TeacherId!.Value,
            GradeId = gradeId,
            Status = "Pending",
            RequestDate = DateTime.UtcNow
        };

        db.RecheckRequests.Add(request);
        await db.SaveChangesAsync();

        return (true, "Dispute submitted successfully.");
    }

    public async Task<(bool success, string message)> UpdateDisputeStatusAsync(int requestId, int teacherId, string status, string? comment)
    {
        var request = await db.RecheckRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.TeacherId == teacherId);

        if (request == null) return (false, "Dispute not found.");
        if (request.Status != "Pending") return (false, "This dispute has already been resolved.");

        request.Status = status;
        request.TeacherComment = comment;
        
        await db.SaveChangesAsync();
        return (true, $"Dispute {status.ToLower()} successfully.");
    }
}
