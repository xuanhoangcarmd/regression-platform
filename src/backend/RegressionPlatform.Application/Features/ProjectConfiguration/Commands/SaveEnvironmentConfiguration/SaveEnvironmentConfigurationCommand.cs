using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ProjectConfiguration.Commands.SaveEnvironmentConfiguration;

/// <summary>
/// A header to save. For secrets, <see cref="ValueProvided"/> is false when the user kept the
/// stored (masked) value unchanged — the existing encrypted value is preserved, not overwritten.
/// </summary>
public sealed record ConfigurationHeaderInput(string Key, string? Value, bool IsSecret, bool ValueProvided);

/// <summary>Upserts a project's configuration for one environment (Base URL + headers).</summary>
public sealed record SaveEnvironmentConfigurationCommand(
    string ProjectCode,
    string Environment,
    string? BaseUrl,
    IReadOnlyList<ConfigurationHeaderInput> Headers
) : IRequest<Result>;
