namespace RegressionPlatform.Application.Features.ProjectConfiguration.Contracts;

/// <summary>
/// A header in a configuration response. For secret headers <see cref="Value"/> is always
/// <c>null</c> (never returned) and <see cref="HasValue"/> indicates whether a secret is stored.
/// </summary>
public sealed record ConfigurationHeaderResponse(string Key, string? Value, bool IsSecret, bool HasValue);

/// <summary>An environment's configuration: Base URL + headers.</summary>
public sealed record EnvironmentConfigurationResponse(
    string Environment,
    string? BaseUrl,
    IReadOnlyList<ConfigurationHeaderResponse> Headers
);
