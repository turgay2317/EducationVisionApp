using EducationVisionApp.Application.DTOs.Lesson;
using EducationVisionApp.Domain.Entities;

namespace EducationVisionApp.Application.DTOs.Class;

public class ClassListDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<LessonListDto> Lessons { get; set; }
}
