using EduTrack.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Data;
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<RecheckRequest> RecheckRequests => Set<RecheckRequest>();
    public DbSet<AtRiskFlag> AtRiskFlags => Set<AtRiskFlag>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Student>().HasIndex(x => x.RollNumber).IsUnique();
        builder.Entity<Student>().HasIndex(x => x.Email).IsUnique();
        builder.Entity<Teacher>().HasIndex(x => x.EmployeeId).IsUnique();
        builder.Entity<Teacher>().HasIndex(x => x.Email).IsUnique();
        builder.Entity<Course>().HasIndex(x => x.CourseCode).IsUnique();
        builder.Entity<Course>().Property(x => x.CreditHours).HasPrecision(3, 1);
        builder.Entity<Course>().HasOne(x => x.Teacher).WithMany(x => x.Courses).HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Student>().HasOne(x => x.ApplicationUser).WithOne().HasForeignKey<Student>(x => x.ApplicationUserId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Teacher>().HasOne(x => x.ApplicationUser).WithOne().HasForeignKey<Teacher>(x => x.ApplicationUserId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Enrollment>().HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
        builder.Entity<Enrollment>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Enrollment>().HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Grade>().HasIndex(x => x.EnrollmentId).IsUnique();
        builder.Entity<Grade>().HasOne(x => x.Enrollment).WithOne(x => x.Grade).HasForeignKey<Grade>(x => x.EnrollmentId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RecheckRequest>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RecheckRequest>().HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RecheckRequest>().HasOne(x => x.Grade).WithMany().HasForeignKey(x => x.GradeId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AtRiskFlag>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
    }
}
