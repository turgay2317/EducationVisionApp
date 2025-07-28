namespace EducationVisionApp.Domain.Entities;

public class StudentClassRecord
{
    public long Id { get; set; }
    public long ClassId { get; set; }
    public long StudentId { get; set; }
    public required Student Student { get; set; }
    public required Class Class { get; set; }
}