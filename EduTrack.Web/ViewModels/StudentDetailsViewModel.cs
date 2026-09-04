using EduTrack.Web.Models;

namespace EduTrack.Web.ViewModels;

public record StudentDetailsViewModel(Student Student, IReadOnlyList<Enrollment> Enrollments)
{
    public int ActiveCourseCount => Enrollments.Count(x => x.IsActive);
    public int GradedCourseCount => Enrollments.Count(x => x.Grade is not null);
}
