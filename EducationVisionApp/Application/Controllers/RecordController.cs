using EducationVisionApp.Application.DTOs.Record;
using EducationVisionApp.Bussines.Services.Abstract;
using EducationVisionApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EducationVisionApp.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordController
{
    private readonly IRecordService _recordService;
    public RecordController(IRecordService recordService)
    {
        _recordService = recordService;
    }

    [HttpPost]
    public async Task<bool> Add([FromBody] RecordCreateDto dto)
    {
        return await _recordService.AddAsync(dto);
    }
    
    [HttpGet("{lessonId}")]
    public StudentRecordAverageDto Details(long lessonId)
    {
        return _recordService.GetAverageRecordsOfStudentsByLesson(lessonId);
    }
    
    [HttpGet]
    public List<ClassLessonDto> AllClassesAndLessons()
    {
        return _recordService.GetAll();
    }
    
    [HttpGet("My")]
    public List<ClassLessonDto> GetMyLessons()
    {
        return _recordService.GetMine();
    }
}
