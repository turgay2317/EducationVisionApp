using AutoMapper;
using EducationVisionApp.Application.DTOs.Student;
using EducationVisionApp.Bussines.Services.Abstract;
using EducationVisionApp.Data.Context;
using EducationVisionApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EducationVisionApp.Bussines.Services;

public class StudentService : IStudentService
{
    private readonly EducationDbContext _context;
    private readonly IMapper _mapper;

    public StudentService(EducationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public async Task<StudentListDto> CreateAsync(StudentCreateUpdateDto dto)
    {
        var student = _mapper.Map<StudentCreateUpdateDto, Student>(dto);
        await _context.AddAsync(student);
        await _context.SaveChangesAsync();
        return _mapper.Map<Student, StudentListDto>(student);
    } 
    
    public async Task<StudentListDto> GetAsync(long id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null) throw new Exception("İlgili öğrenci bulunamadı");
        return _mapper.Map<Student, StudentListDto>(student);
    }
    
    public async Task<StudentListDto> UpdateAsync(long id, StudentCreateUpdateDto dto)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null) throw new Exception("İlgili öğrenci bulunamadı");
        _mapper.Map(dto, student);
        await _context.SaveChangesAsync();
        return _mapper.Map<Student, StudentListDto>(student);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null) return false;
        _context.Students.Remove(student);
        return await _context.SaveChangesAsync() > 0;
    }
}