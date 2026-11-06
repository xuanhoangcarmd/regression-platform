namespace RegressionPlatform.Application.Features.Report.Ai;

/// <summary>Selects the AI provider to use, based on <c>AI:Provider</c> configuration.</summary>
public interface IAiProviderResolver
{
    /// <summary>The provider selected by configuration (falls back to rule-based if unknown).</summary>
    IAiProvider Resolve();

    /// <summary>The always-available deterministic provider (rule-based) — used as a safety fallback.</summary>
    IAiProvider Fallback();
}
