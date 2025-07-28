using System.ComponentModel.DataAnnotations;

namespace EducationVisionApp.Domain.Entities;

public class Class
{
    public long Id { get; set; }
    public string Name { get; set; }
    public long TeacherId { get; set; }
    public User Teacher { get; set; }
}