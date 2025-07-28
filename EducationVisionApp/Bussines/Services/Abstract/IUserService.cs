using EducationVisionApp.Application.DTOs.Student;

namespace EducationVisionApp.Bussines.Services.Abstract;

public interface IUserService
{
    public Task<UserListDto> CreateAsync(UserCreateUpdateDto dto);
    public Task<UserListDto> GetAsync(long id);
    public Task<UserListDto> UpdateAsync(long id, UserCreateUpdateDto dto);
    public Task<bool> DeleteAsync(long id);
}