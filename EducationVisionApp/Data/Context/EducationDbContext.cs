using EducationVisionApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace EducationVisionApp.Data.Context;

public class EducationDbContext : DbContext
{
    public DbSet<Student> Students { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Record> Records { get; set; }
    public DbSet<StudentClassRecord> StudentClassRecords { get; set; }

    public EducationDbContext(DbContextOptions<EducationDbContext> options) : base(options)
    {
        
    }
}