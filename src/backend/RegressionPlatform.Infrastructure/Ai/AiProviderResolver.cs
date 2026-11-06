using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;

namespace RegressionPlatform.Infrastructure.Ai;

/// <summary>Picks the <see cref="IAiProvider"/> named by <c>AI:Provider</c>; rule-based otherwise.</summary>
public sealed class AiProviderResolver(IEnumerable<IAiProvider> providers, AiOptions options) : IAiProviderResolver
{
    public IAiProvider Resolve() =>
        providers.FirstOrDefault(provider => string.Equals(provider.Name, options.Provider, StringComparison.OrdinalIgnoreCase))
        ?? Fallback();

    public IAiProvider Fallback() =>
        providers.First(provider => provider.Name == AiProviderNames.RuleBased);
}
