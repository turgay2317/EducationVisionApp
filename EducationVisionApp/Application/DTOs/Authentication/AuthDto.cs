using System.ComponentModel.DataAnnotations;

namespace EducationVisionApp.Application.DTOs.Authentication;

public class AuthDto
{
    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
}