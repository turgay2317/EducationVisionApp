using EducationVisionApp.Application.DTOs.Lesson;
using EducationVisionApp.Bussines.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace EducationVisionApp.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LessonController
{
    private readonly ILessonService _lessonService;

    public LessonController(ILessonService lessonService)
    {
        _lessonService = lessonService;
    }
    
    [HttpPost]
    public Task<LessonListDto> Add(CreateLessonDto dto)
    {
        return _lessonService.CreateLesson(dto);
    }
}