using EducationVisionApp.Domain.Enums;

namespace EducationVisionApp.Application.DTOs.Lesson;

public class CreateLessonDto
{
    public int ClassId { get; set; }
    public int TeacherId { get; set; }
    public string Name { get; set; }
    public LessonType Type { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}