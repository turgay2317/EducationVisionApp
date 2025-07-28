using EducationVisionApp.Application.DTOs.Authentication;
using EducationVisionApp.Domain.Entities;

namespace EducationVisionApp.Bussines.Services.Abstract;

public interface IAuthenticationService
{
    public Task<string> Authenticate(AuthDto authDto);
    public Teacher? GetCurrentUser();
}