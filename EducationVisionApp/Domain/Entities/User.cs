using EducationVisionApp.Domain.Enums;

namespace EducationVisionApp.Domain.Entities;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public UserType Type { get; set; }
}