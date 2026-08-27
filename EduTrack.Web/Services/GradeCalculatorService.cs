using EduTrack.Web.Models;
namespace EduTrack.Web.Services;

/// <summary>
/// Calculates the total mark, letter grade, and grade point for a <see cref="Grade"/>.
/// Component weights (of the 0-100 total): Assignment/Quiz 20%, Attendance 10%, Midterm 20%, Final 50%.
/// </summary>
public class GradeCalculatorService
{
    public const decimal AssignmentWeight = 0.20m;
    public const decimal AttendanceWeight = 0.10m;
    public const decimal MidtermWeight = 0.20m;
    public const decimal FinalWeight = 0.50m;

    public decimal CalculateTotalMark(Grade grade) =>
        Math.Round(
            grade.AssignmentMark * AssignmentWeight +
            grade.AttendanceMark * AttendanceWeight +
            grade.MidtermMark * MidtermWeight +
            grade.FinalMark * FinalWeight,
            2, MidpointRounding.AwayFromZero);

    public (string LetterGrade, decimal GradePoint) CalculateLetterGradeAndPoint(decimal totalMark) => totalMark switch
    {
        >= 80 => ("A+", 4.00m),
        >= 75 => ("A", 3.75m),
        >= 70 => ("A-", 3.50m),
        >= 65 => ("B+", 3.25m),
        >= 60 => ("B", 3.00m),
        >= 55 => ("B-", 2.75m),
        >= 50 => ("C+", 2.50m),
        >= 45 => ("C", 2.25m),
        >= 40 => ("D", 2.00m),
        _ => ("F", 0.00m),
    };

    /// <summary>
    /// Recalculates and applies TotalMark, LetterGrade, GradePoint, and UpdatedAt onto the given grade.
    /// </summary>
    public void Apply(Grade grade)
    {
        grade.TotalMark = CalculateTotalMark(grade);
        var (letterGrade, gradePoint) = CalculateLetterGradeAndPoint(grade.TotalMark);
        grade.LetterGrade = letterGrade;
        grade.GradePoint = gradePoint;
        grade.UpdatedAt = DateTime.UtcNow;
    }
}
