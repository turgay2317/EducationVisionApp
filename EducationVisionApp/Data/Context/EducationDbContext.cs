using EducationVisionApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace EducationVisionApp.Data.Context;

public class EducationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Record> Records { get; set; }
    public DbSet<UserClass> UserClasses { get; set; }
    public DbSet<UserClassRecord> UserClassRecords { get; set; }

    public EducationDbContext(DbContextOptions<EducationDbContext> options) : base(options)
    {
        
    }
}