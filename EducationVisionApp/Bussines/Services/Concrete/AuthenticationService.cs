using EducationVisionApp.Application.DTOs.Authentication;
using EducationVisionApp.Bussines.Services.Abstract;
using EducationVisionApp.Data.Context;
using EducationVisionApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationVisionApp.Bussines.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly EducationDbContext _context;

    public AuthenticationService(IHttpContextAccessor httpContextAccessor, EducationDbContext context, IJwtTokenService jwtTokenService)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public long? GetCurrentUserId()
    {
        return long.Parse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    public User? GetCurrentUser()
    {
        var teacherId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(teacherId))
            return null;
        return _context.Users.FirstOrDefault(t => t.Id.ToString() == teacherId);
    }

    public async Task<string> Authenticate(AuthDto authDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == authDto.Email && x.Password == authDto.Password);
        if (user == null)
            throw new UnauthorizedAccessException("E-posta ya da şifre yanlılş");

        return _jwtTokenService.GenerateToken(user.Id);
    }
}