namespace EducationVisionApp.Domain.Entities;

public class Class
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<Lesson> Lessons { get; set; }
    public List<User> Students { get; set; }
}