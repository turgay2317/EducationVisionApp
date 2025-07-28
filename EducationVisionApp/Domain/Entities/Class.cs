using System.ComponentModel.DataAnnotations;

namespace EducationVisionApp.Domain.Entities;

public class Class
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public long TeacherId { get; set; }
    public required Teacher Teacher { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
}