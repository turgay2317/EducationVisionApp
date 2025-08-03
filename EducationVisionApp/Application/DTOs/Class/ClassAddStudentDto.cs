using EducationVisionApp.Application.DTOs.Record;
using EducationVisionApp.Application.DTOs.Student;

namespace EducationVisionApp.Application.DTOs.Class;

public class ClassAddStudentDto
{
    public long Id { get; set; }
    public List<UserListDto> Students { get; set; }
}
