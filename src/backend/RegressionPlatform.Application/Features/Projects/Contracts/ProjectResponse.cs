namespace RegressionPlatform.Application.Features.Projects.Contracts;

/// <summary>Response DTO for a project (used by the nav/project switcher).</summary>
public sealed record ProjectResponse(int Id, string Code, string Name);
