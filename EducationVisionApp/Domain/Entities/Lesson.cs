namespace EducationVisionApp.Domain.Entities;

public class Lesson
{
    public long Id { get; set; }
    public string Name { get; set; }
    public long ClassId { get; set; }
    public long TeacherId { get; set; }
    public User Teacher { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsFinished { get; set; }
    public List<Record> Records { get; set; }
    public string Comment { get; set; }
    public string CommentForNextTeacher { get; set; }

}