namespace EducationVisionApp.Bussines.Services.Abstract;

public interface IJwtTokenService
{
    string GenerateToken(long userId);
}