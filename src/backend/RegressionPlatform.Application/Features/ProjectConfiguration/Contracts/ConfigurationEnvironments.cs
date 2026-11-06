namespace RegressionPlatform.Application.Features.ProjectConfiguration.Contracts;

/// <summary>The environments a project can be configured for (matches the DB CHECK constraint).</summary>
public static class ConfigurationEnvironments
{
    public static readonly IReadOnlyList<string> All = ["DEV", "STG", "PROD"];
}
