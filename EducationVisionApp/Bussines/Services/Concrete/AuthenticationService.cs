using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EducationVisionApp.Application.DTOs.Authentication;
using EducationVisionApp.Bussines.Services.Abstract;
using EducationVisionApp.Data.Context;
using EducationVisionApp.Domain;
using EducationVisionApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EducationVisionApp.Bussines.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly EducationDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthenticationService(IHttpContextAccessor httpContextAccessor, EducationDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public Teacher? GetCurrentUser()
    {
        var teacherId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(teacherId)) 
            return null;
        return _context.Teachers.FirstOrDefault(t => t.Id.ToString() == teacherId);
    }

    private string GetToken(Teacher teacher)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, teacher.Id.ToString()),
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<string> Authenticate(AuthDto authDto)
    {
        var teacher = await _context.Teachers.FirstOrDefaultAsync(x => x.Email == authDto.Email && x.Password == authDto.Password);
        if (teacher == null)
            throw new UnauthorizedAccessException("E-posta ya da şifre yanlılş");
        
        return GetToken(teacher);
    }
}