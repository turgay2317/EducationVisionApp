using System.ComponentModel.DataAnnotations;

namespace EducationVisionApp.Application.DTOs.Student;

public class UserCreateUpdateDto
{
    public long Id { get; set; }
    [Required(ErrorMessage = "Öğrenci ismi zorunludur!")]
    public string Name { get; set; }
    [RegularExpression(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$", 
        ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; }
    [MinLength(6, ErrorMessage = "Şifreniz en az 6 haneli olmalıdır")]
    public string Password { get; set; }
}