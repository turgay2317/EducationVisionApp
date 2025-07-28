using System.ComponentModel.DataAnnotations;

namespace EducationVisionApp.Application.DTOs.Student;

public class StudentCreateUpdateDto
{
    public long Id { get; set; }
    [Required(ErrorMessage = "Öğrenci ismi zorunludur!")]
    public string Name { get; set; }
}