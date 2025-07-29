using EducationVisionApp.Application.DTOs.Student;

namespace EducationVisionApp.Application.DTOs.Class;

public class ClassListDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<UserListDto> Students { get; set; }
}
