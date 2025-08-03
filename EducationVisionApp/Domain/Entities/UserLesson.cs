namespace EducationVisionApp.Domain.Entities;

public class UserLesson
{
    public long Id { get; set; }
    public long LessonId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; }
    public Lesson Lesson { get; set; }
    public float AvgDistracted { get; set; }
    public float AvgFocused { get; set; }
    public float AvgSleepy { get; set; }
    public List<Record> Records { get; set; }
}