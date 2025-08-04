namespace EducationVisionApp.Application.DTOs.Lesson;

public class CreateLessonDto
{
    public int ClassId { get; set; }
    public int TeacherId { get; set; }
    public string Name { get; set; }
}