using AutoMapper;
using EducationVisionApp.Application.DTOs.Authentication;
using EducationVisionApp.Bussines.Services.Abstract;
using EducationVisionApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IMapper _mapper;

    public AuthenticationController(IAuthenticationService authenticationService, IMapper mapper)
    {
        _authenticationService = authenticationService;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<string> Login([FromBody] AuthDto authDto)
    {
        return await _authenticationService.Authenticate(authDto);
    }

    [HttpGet]
    [Authorize]
    public User? Get()
    {
        return _authenticationService.GetCurrentUser();
    }
}