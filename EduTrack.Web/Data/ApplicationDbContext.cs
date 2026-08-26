using EduTrack.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace EduTrack.Web.Data;
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Course> Courses => Set<Course>();
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
    }
}
