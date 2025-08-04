using EducationVisionApp.Application.DTOs.Lesson;

namespace EducationVisionApp.Bussines.Services.Abstract;

public interface ILessonService
{
    public Task<LessonListDto> CreateLesson(CreateLessonDto dto);
}