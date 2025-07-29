using AutoMapper;
using EducationVisionApp.Application.DTOs.Class;
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

    public async Task<List<ClassListDto>> GetAllAsync()
    {
        var classes = await _context.Classes
            .Where(c => c.TeacherId == _authenticationService.GetCurrentUserId())
            .ToListAsync();
        // TO-DO: Düzelt
        return null;
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
