using AutoMapper;
using EducationVisionApp.Application.DTOs.Student;
using EducationVisionApp.Bussines.Services.Abstract;
using EducationVisionApp.Data.Context;
using EducationVisionApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EducationVisionApp.Bussines.Services;

public class UserService : IUserService
{
    private readonly EducationDbContext _context;
    private readonly IMapper _mapper;

    public UserService(EducationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public async Task<UserListDto> CreateAsync(UserCreateUpdateDto dto)
    {
        var student = _mapper.Map<UserCreateUpdateDto, User>(dto);
        await _context.AddAsync(student);
        await _context.SaveChangesAsync();
        return _mapper.Map<User, UserListDto>(student);
    } 
    
    public async Task<UserListDto> GetAsync(long id)
    {
        var student = await _context.Users.FindAsync(id);
        if (student == null) throw new Exception("İlgili öğrenci bulunamadı");
        return _mapper.Map<User, UserListDto>(student);
    }
    
    public async Task<UserListDto> UpdateAsync(long id, UserCreateUpdateDto dto)
    {
        var student = await _context.Users.FindAsync(id);
        if (student == null) throw new Exception("İlgili öğrenci bulunamadı");
        _mapper.Map(dto, student);
        await _context.SaveChangesAsync();
        return _mapper.Map<User, UserListDto>(student);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var student = await _context.Users.FindAsync(id);
        if (student == null) return false;
        _context.Users.Remove(student);
        return await _context.SaveChangesAsync() > 0;
    }
}