using EducationVisionApp.Application.DTOs.Prompt;
using EducationVisionApp.Data;
using Microsoft.AspNetCore.Mvc;

namespace EducationVisionApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeminiController : ControllerBase
{
    private readonly GeminiClient _geminiClient;

    public GeminiController(GeminiClient geminiClient)
    {
        _geminiClient = geminiClient;
    }

    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] PromptRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest("Prompt cannot be empty.");

        var result = await _geminiClient.GenerateContentAsync(request.Prompt, cancellationToken);
        return Ok(new { response = result });
    }
}