using EducationVisionApp.Application.DTOs.Class;

namespace EducationVisionApp.Bussines.Services.Abstract;

public interface IClassService
{
    public Task<List<ClassListDto>> GetAllAsync();
    public Task<ClassListDto> CreateAsync(ClassCreateUpdateDto dto);
    public Task<ClassListDto> UpdateAsync(long id, ClassCreateUpdateDto dto);
    public Task<ClassListDto> AddStudentAsync(long id, ClassAddStudentDto dto);
    public Task<bool> DeleteAsync(long id);
}
