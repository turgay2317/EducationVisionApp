using EducationVisionApp.Application.DTOs.Class;
using EducationVisionApp.Bussines.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EducationVisionApp.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClassController : ControllerBase
{
    private readonly IClassService _classService;

    public ClassController(IClassService classService)
    {
        _classService = classService;
    }

    [HttpGet]
    public Task<List<ClassListDto>> GetAll()
    {
        return _classService.GetAllAsync();
    }

    [HttpPost("{id}")]
    public Task<ClassListDto> AddStudent(long id, [FromBody] ClassAddStudentDto dto)
    {
        return _classService.AddStudentAsync(id, dto);
    }

    [HttpPost]
    public Task<ClassListDto> Create([FromBody] ClassCreateUpdateDto dto)
    {
        return _classService.CreateAsync(dto);
    }

    [HttpPut("{id}")]
    public Task<ClassListDto> Update(long id, [FromBody] ClassCreateUpdateDto dto)
    {
        return _classService.UpdateAsync(id, dto);
    }

    [HttpDelete("{id}")]
    public Task<bool> Delete(long id)
    {
        return _classService.DeleteAsync(id);
    }
}
