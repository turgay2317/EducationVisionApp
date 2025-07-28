using EducationVisionApp.Application.DTOs.Student;
using EducationVisionApp.Bussines.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace EducationVisionApp.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;
    
    public StudentController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpPost]
    public async Task<StudentListDto> CreateAsync([FromBody] StudentCreateUpdateDto dto)
    {
        return await _studentService.CreateAsync(dto);
    }

    [HttpGet("{id}")]
    public async Task<StudentListDto> GetAsync(long id)
    {
        return await _studentService.GetAsync(id);
    }

    [HttpPut("{id}")]
    public async Task<StudentListDto> UpdateAsync(long id, [FromBody] StudentCreateUpdateDto dto)
    {
        return await _studentService.UpdateAsync(id, dto);
    }

    [HttpDelete("{id}")]
    public async Task<bool> DeleteAsync(long id)
    {
        return await _studentService.DeleteAsync(id);
    }
}