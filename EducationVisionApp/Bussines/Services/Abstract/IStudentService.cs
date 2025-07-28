using EducationVisionApp.Application.DTOs.Student;

namespace EducationVisionApp.Bussines.Services.Abstract;

public interface IStudentService
{
    public Task<StudentListDto> CreateAsync(StudentCreateUpdateDto dto);
    public Task<StudentListDto> GetAsync(long id);
    public Task<StudentListDto> UpdateAsync(long id, StudentCreateUpdateDto dto);
    public Task<bool> DeleteAsync(long id);
}