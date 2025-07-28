using EducationVisionApp.Application.DTOs.Student;
using EducationVisionApp.Bussines.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace EducationVisionApp.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<UserListDto> CreateAsync([FromBody] UserCreateUpdateDto dto)
    {
        return await _userService.CreateAsync(dto);
    }

    [HttpGet("{id}")]
    public async Task<UserListDto> GetAsync(long id)
    {
        return await _userService.GetAsync(id);
    }

    [HttpPut("{id}")]
    public async Task<UserListDto> UpdateAsync(long id, [FromBody] UserCreateUpdateDto dto)
    {
        return await _userService.UpdateAsync(id, dto);
    }

    [HttpDelete("{id}")]
    public async Task<bool> DeleteAsync(long id)
    {
        return await _userService.DeleteAsync(id);
    }
}