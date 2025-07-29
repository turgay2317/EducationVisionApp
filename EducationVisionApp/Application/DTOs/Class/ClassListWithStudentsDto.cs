using EducationVisionApp.Application.DTOs.Student;

namespace EducationVisionApp.Application.DTOs.Class;

public class ClassListWithStudentsDto : ClassListDto
{
    public List<UserListDto> Students { get; set; }
}
