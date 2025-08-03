using EducationVisionApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace EducationVisionApp.Data.Context;

public class EducationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Record> Records { get; set; }
    public DbSet<UserLesson> UserLessons { get; set; }

    public EducationDbContext(DbContextOptions<EducationDbContext> options) : base(options)
    {
        
    }
}