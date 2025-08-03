namespace EducationVisionApp.Application.DTOs.Record;

public class LessonDto
{
    public string LessonName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsFinished { get; set; }
    public string TeacherName { get; set; }
    public float AvgDistracted { get; set; }
    public float AvgSleepy { get; set; }
    public float AvgFocused { get; set; }
}

public class ClassLessonDto
{
    public string ClassName { get; set; }
    public List<LessonDto> Lessons { get; set; }
}