using EducationVisionApp.Domain.Enums;

namespace EducationVisionApp.Application.DTOs.Authentication;

public class AuthTokenDto
{
    public string Token { get; set; }
    public UserType Type { get; set; }
}