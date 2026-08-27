using EduTrack.Web.Models;
namespace EduTrack.Web.Services;

/// <summary>
/// Calculates the total mark, letter grade, and grade point for a <see cref="Grade"/>.
/// Component marks are summed directly out of 100: Assignment/Quiz 0-20, Attendance 0-10, Midterm 0-20, Final 0-50.
/// </summary>
public class GradeCalculatorService
{
    public const decimal AssignmentMaxMark = 20m;
    public const decimal AttendanceMaxMark = 10m;
    public const decimal MidtermMaxMark = 20m;
    public const decimal FinalMaxMark = 50m;

    public decimal CalculateTotalMark(Grade grade) =>
        Math.Round(
            grade.AssignmentMark +
            grade.AttendanceMark +
            grade.MidtermMark +
            grade.FinalMark,
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
