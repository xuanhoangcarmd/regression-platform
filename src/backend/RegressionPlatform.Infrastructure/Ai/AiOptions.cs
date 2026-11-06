using RegressionPlatform.Application.Features.Report.Ai.Contracts;

namespace RegressionPlatform.Infrastructure.Ai;

/// <summary>Bound from the <c>AI</c> configuration section. Selects the provider + its credentials.</summary>
public sealed class AiOptions
{
    public const string SectionName = "AI";

    /// <summary>Active provider name (see <see cref="AiProviderNames"/>). Defaults to rule-based.</summary>
    public string Provider { get; init; } = AiProviderNames.RuleBased;

    public DeepSeekOptions DeepSeek { get; init; } = new();
}

/// <summary>DeepSeek API settings (OpenAI-compatible chat completions).</summary>
public sealed class DeepSeekOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api.deepseek.com";
    public string Model { get; init; } = "deepseek-chat";
}
