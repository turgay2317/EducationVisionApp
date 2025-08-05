using AutoMapper;
using EducationVisionApp.Application.DTOs.Lesson;
using EducationVisionApp.Bussines.Services.Abstract;
using EducationVisionApp.Data.Context;
using EducationVisionApp.Domain.Entities;

namespace EducationVisionApp.Bussines.Services.Concrete;

public class LessonService : ILessonService
{
    private readonly EducationDbContext _context;
    private readonly IMapper _mapper;

    public LessonService(EducationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public async Task<LessonListDto> CreateLesson(CreateLessonDto dto)
    {
        var entity = _mapper.Map<Lesson>(dto);
        _context.Lessons.Add(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<LessonListDto>(entity);
    }
}