namespace EducationVisionApp.Data;

internal sealed class GeminiRequest
{
    public GeminiContent[] Contents { get; set; }
    public GenerationConfig GenerationConfig { get; set; }
    public SafetySetting[] SafetySettings { get; set; }
}

internal sealed class GeminiContent
{
    public string Role { get; set; }
    public GeminiPart[] Parts { get; set; }
}

internal sealed class GeminiPart
{
    // This one interests us the most
    public string Text { get; set; }
}

// Two models used for configuration
internal sealed class GenerationConfig
{
    public int Temperature { get; set; }
    public int TopK { get; set; }
    public int TopP { get; set; }
    public int MaxOutputTokens { get; set; }
    public List<object> StopSequences { get; set; }
}

internal sealed class SafetySetting
{
    public string Category { get; set; }
    public string Threshold { get; set; }
}

internal sealed class GeminiResponse
{
    public Candidate[] Candidates { get; set; }
    public PromptFeedback PromptFeedback { get; set; }
}

internal sealed class PromptFeedback
{
    public SafetyRating[] SafetyRatings { get; set; }
}

internal sealed class Candidate
{
    public Content Content { get; set; }
    public string FinishReason { get; set; }
    public int Index { get; set; }
    public SafetyRating[] SafetyRatings { get; set; }
}

internal sealed class Content
{
    public Part[] Parts { get; set; }
    public string Role { get; set; }
}

internal sealed class Part
{
    // This one interests us the most
    public string Text { get; set; }
}

internal sealed class SafetyRating
{
    public string Category { get; set; }
    public string Probability { get; set; }
}