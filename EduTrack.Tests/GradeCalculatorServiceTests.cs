using EduTrack.Web.Models;
using EduTrack.Web.Services;
using Xunit;

namespace EduTrack.Tests;

public class GradeCalculatorServiceTests
{
    private readonly GradeCalculatorService _calculator = new();

    [Fact]
    public void CalculateTotalMark_ReturnsSumOfMarks()
    {
        var grade = new Grade
        {
            MidtermMark = 18,
            FinalMark = 42,
            AssignmentMark = 15,
            AttendanceMark = 8
        };

        var total = _calculator.CalculateTotalMark(grade);
        Assert.Equal(83m, total);
    }

    [Theory]
    [InlineData(85, "A+", 4.00)]
    [InlineData(80, "A+", 4.00)]
    [InlineData(78, "A", 3.75)]
    [InlineData(75, "A", 3.75)]
    [InlineData(72, "A-", 3.50)]
    [InlineData(70, "A-", 3.50)]
    [InlineData(67, "B+", 3.25)]
    [InlineData(65, "B+", 3.25)]
    [InlineData(62, "B", 3.00)]
    [InlineData(60, "B", 3.00)]
    [InlineData(57, "B-", 2.75)]
    [InlineData(55, "B-", 2.75)]
    [InlineData(52, "C+", 2.50)]
    [InlineData(50, "C+", 2.50)]
    [InlineData(47, "C", 2.25)]
    [InlineData(45, "C", 2.25)]
    [InlineData(42, "D", 2.00)]
    [InlineData(40, "D", 2.00)]
    [InlineData(39, "F", 0.00)]
    [InlineData(0, "F", 0.00)]
    public void CalculateLetterGradeAndPoint_MapsMarksCorrectly(decimal totalMark, string expectedGrade, decimal expectedPoint)
    {
        var (letterGrade, gradePoint) = _calculator.CalculateLetterGradeAndPoint(totalMark);
        Assert.Equal(expectedGrade, letterGrade);
        Assert.Equal(expectedPoint, gradePoint);
    }

    [Fact]
    public void Apply_UpdatesGradeObjectProperties()
    {
        var grade = new Grade
        {
            MidtermMark = 16,
            FinalMark = 44,
            AssignmentMark = 18,
            AttendanceMark = 9
        };

        _calculator.Apply(grade);

        Assert.Equal(87m, grade.TotalMark);
        Assert.Equal("A+", grade.LetterGrade);
        Assert.Equal(4.00m, grade.GradePoint);
    }
}
