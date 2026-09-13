using EduTrack.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Web.Services;

public class CgpaCalculationService(ApplicationDbContext db)
{
    public async Task<decimal> RecalculateStudentCgpaAsync(int studentId)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
        if (student == null) return 0m;

        var enrollments = await db.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Grade)
            .Where(e => e.StudentId == studentId)
            .ToListAsync();

        decimal totalPoints = 0;
        decimal totalCredits = 0;

        foreach (var enrollment in enrollments)
        {
            if (enrollment.Grade != null && enrollment.Course != null)
            {
                totalPoints += enrollment.Grade.GradePoint * enrollment.Course.CreditHours;
                totalCredits += enrollment.Course.CreditHours;
            }
        }

        decimal cgpa = totalCredits > 0 ? Math.Round(totalPoints / totalCredits, 2) : 0;
        
        student.CGPA = cgpa;
        await db.SaveChangesAsync();

        return cgpa;
    }
}
