using AutoMapper;
using EducationVisionApp.Application.DTOs.Class;
using EducationVisionApp.Application.DTOs.Student;
using EducationVisionApp.Bussines.Services.Abstract;
using EducationVisionApp.Data.Context;
using EducationVisionApp.Domain.Entities;

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
        var classes = _context.Classes
            .Where(c => c.TeacherId == _authenticationService.GetCurrentUserId())
            .ToList();

        var classesAndStudents = classes.Select(c =>
        {
            var students = _context.UserClasses
            .Where(uc => uc.ClassId == c.Id)
            .Select(uc => uc.User)
            .ToList();

            return new ClassListWithStudentsDto
            {
                Id = c.Id,
                Name = c.Name,
                Students = _mapper.Map<List<UserListDto>>(students)
            };
        }).ToList();

        return classesAndStudents;
    }

    public async Task<ClassListDto> CreateAsync(ClassCreateUpdateDto dto)
    {
        var entity = _mapper.Map<Class>(dto);
        entity.TeacherId = _authenticationService.GetCurrentUser().Id;
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
        var entity = await _context.Classes.FindAsync(id);
        if (entity == null) throw new Exception("İlgili sınıf bulunamadı");
        await _context.UserClasses.AddRangeAsync(
            dto.StudentIds.Select(studentId => new UserClass { ClassId = id, UserId = studentId }
        ));
        await _context.SaveChangesAsync();
        return _mapper.Map<ClassListDto>(entity);
    }
}
