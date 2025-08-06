using AutoMapper;
using EducationVisionApp.Application.DTOs.Class;
using EducationVisionApp.Application.DTOs.Lesson;
using EducationVisionApp.Application.DTOs.Student;
using EducationVisionApp.Bussines.Services.Abstract;
using EducationVisionApp.Data.Context;
using EducationVisionApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EducationVisionApp.Bussines.Services.Concrete;

public class ClassService : IClassService
{
    private readonly EducationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAuthenticationService _authenticationService;

    public ClassService(EducationDbContext context, IMapper mapper, IAuthenticationService authenticationService)
    {
        this._context = context;
        this._mapper = mapper;
        this._authenticationService = authenticationService;
    }

    public async Task<List<ClassListWithStudentsDto>> GetAllAsync()
    {
        var classes = await _context.Classes
            .Include(x => x.Students)
            .Include(x => x.Lessons)
            .ToListAsync();

        foreach (var cls in classes)
        {
            cls.Lessons = cls.Lessons
                .OrderByDescending(l => l.EndTime) 
                .ToList();
        }
        
        var classesAndStudents = classes.Select(c =>
        {
            var lessonIds = _context.Lessons
                .Where(x => x.ClassId == c.Id)
                .Select(x => x.Id)
                .ToList();

            var studentCount = _context.UserLessons
                .Where(x => lessonIds.Contains(x.LessonId))
                .Distinct()
                .Count();

            return new ClassListWithStudentsDto
            {
                Id = c.Id,
                Name = c.Name,
                studentCount = studentCount / c.Lessons.Count(),
                Lessons = _mapper.Map<List<LessonListDto>>(c.Lessons)
            };
        }).ToList();

        return classesAndStudents;
    }

    public async Task<ClassListDto> CreateAsync(ClassCreateUpdateDto dto)
    {
        var entity = _mapper.Map<Class>(dto);
        await _context.Classes.AddAsync(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<ClassListDto>(entity);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var entity = await _context.Classes.FindAsync(id);
        if (entity == null) return false;
        _context.Classes.Remove(entity);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<ClassListDto> UpdateAsync(long id, ClassCreateUpdateDto dto)
    {
        var entity = await _context.Classes.FindAsync(id);
        if (entity == null) throw new Exception("İlgili sınıf bulunamadı");
        _context.Classes.Update(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<ClassListDto>(entity);
    }

    public async Task<ClassListDto> AddStudentAsync(long id, ClassAddStudentDto dto)
    {
        var entity = await _context.Classes
            .Where(x => x.Id == id)
            .Include(x => x.Students)
            .FirstOrDefaultAsync();
        if (entity == null) throw new Exception("İlgili sınıf bulunamadı");
        entity.Students.AddRange(_mapper.Map<List<User>>(dto.Students));
        await _context.SaveChangesAsync();
        return _mapper.Map<ClassListDto>(entity);
    }
}
