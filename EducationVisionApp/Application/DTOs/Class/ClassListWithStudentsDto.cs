using EducationVisionApp.Application.DTOs.Student;

namespace EducationVisionApp.Application.DTOs.Class;

public class ClassListWithStudentsDto : ClassListDto
{
    public int studentCount { get; set; } = 0;
}
