namespace RegressionPlatform.API.Contracts;

/// <summary>
/// A header in a save request. For secret headers, set <c>ValueProvided=false</c> to keep the
/// stored value unchanged (then <c>Value</c> is ignored), or <c>true</c> with a new <c>Value</c> to change it.
/// </summary>
public sealed record SaveConfigurationHeaderRequest(string Key, string? Value, bool IsSecret, bool ValueProvided);

/// <summary>Request body for saving one environment's configuration (environment + code come from the route).</summary>
public sealed record SaveEnvironmentConfigurationRequest(
    string? BaseUrl,
    IReadOnlyList<SaveConfigurationHeaderRequest>? Headers
);
