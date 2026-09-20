using EduTrack.Web.Models;
using Xunit;

namespace EduTrack.Tests;

public class GradeValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(20)]
    public void AssignmentMark_ValidWithinRange(decimal mark)
    {
        var grade = new Grade { AssignmentMark = mark };
        Assert.InRange(grade.AssignmentMark, 0m, 20m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    public void AssignmentMark_InvalidOutsideRange(decimal mark)
    {
        Assert.False(mark >= 0m && mark <= 20m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void AttendanceMark_ValidWithinRange(decimal mark)
    {
        var grade = new Grade { AttendanceMark = mark };
        Assert.InRange(grade.AttendanceMark, 0m, 10m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void AttendanceMark_InvalidOutsideRange(decimal mark)
    {
        Assert.False(mark >= 0m && mark <= 10m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(20)]
    public void MidtermMark_ValidWithinRange(decimal mark)
    {
        var grade = new Grade { MidtermMark = mark };
        Assert.InRange(grade.MidtermMark, 0m, 20m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    public void MidtermMark_InvalidOutsideRange(decimal mark)
    {
        Assert.False(mark >= 0m && mark <= 20m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(50)]
    public void FinalMark_ValidWithinRange(decimal mark)
    {
        var grade = new Grade { FinalMark = mark };
        Assert.InRange(grade.FinalMark, 0m, 50m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(51)]
    public void FinalMark_InvalidOutsideRange(decimal mark)
    {
        Assert.False(mark >= 0m && mark <= 50m);
    }
}
