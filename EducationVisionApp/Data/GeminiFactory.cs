using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EducationVisionApp.Data;

internal sealed class GeminiRequestFactory
{
    public static GeminiRequest CreateRequest(string prompt)
    {
        return new GeminiRequest
        {
            Contents = new GeminiContent[]
            {
                new GeminiContent
                {
                    Role = "user",
                    Parts = new GeminiPart[]
                    {
                        new GeminiPart
                        {
                            Text = prompt
                        }
                    }
                }
            },
            GenerationConfig = new GenerationConfig
            {
                Temperature = 0,
                TopK = 1,
                TopP = 1,
                MaxOutputTokens = 2048,
                StopSequences = new List<object>()
            },
            SafetySettings = new SafetySetting[]
            {
                new SafetySetting
                {
                    Category = "HARM_CATEGORY_HARASSMENT",
                    Threshold = "BLOCK_ONLY_HIGH"
                },
                new SafetySetting
                {
                    Category = "HARM_CATEGORY_HATE_SPEECH",
                    Threshold = "BLOCK_ONLY_HIGH"
                },
                new SafetySetting
                {
                    Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                    Threshold = "BLOCK_ONLY_HIGH"
                },
                new SafetySetting
                {
                    Category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                    Threshold = "BLOCK_ONLY_HIGH"
                }
            }
        };
    }
}

internal sealed class GeminiDelegatingHandler(IOptions<GeminiOptions> geminiOptions) 
    : DelegatingHandler
{
    private readonly GeminiOptions _geminiOptions = geminiOptions.Value;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("x-goog-api-key", $"{_geminiOptions.ApiKey}");

        return base.SendAsync(request, cancellationToken);
    }
}

public sealed class GeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _serializerSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    public GeminiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateContentAsync(string prompt, CancellationToken cancellationToken)
    {
        var requestBody = GeminiRequestFactory.CreateRequest(prompt);
        var content = new StringContent(JsonConvert.SerializeObject(requestBody, Formatting.None, _serializerSettings), Encoding.UTF8, "application/json");
            
        var response = await _httpClient.PostAsync("", content, cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        var geminiResponse = JsonConvert.DeserializeObject<GeminiResponse>(responseBody);

        var geminiResponseText = geminiResponse?.Candidates[0].Content.Parts[0].Text;

        return geminiResponseText;
    }
}
