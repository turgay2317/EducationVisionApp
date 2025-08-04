namespace EducationVisionApp.Domain.Entities;

public class UserLesson
{
    public long Id { get; set; }
    public long LessonId { get; set; }
    public long StudentId { get; set; }
    public Lesson Lesson { get; set; }
    public float AvgDistracted { get; set; }
    public float AvgFocused { get; set; }
    public float AvgSleepy { get; set; }
    public List<Record> Records { get; set; }
    public int TotalBlinkCount { get; set; }
    public int AvgConfidence { get; set; }
    public int TotalHeadTurn  { get; set; }
    public bool IsProcessed { get; set; }
}