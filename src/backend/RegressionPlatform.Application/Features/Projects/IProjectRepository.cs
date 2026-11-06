using RegressionPlatform.Application.Features.Projects.Contracts;

namespace RegressionPlatform.Application.Features.Projects;

/// <summary>Resolves projects (by route code) for scoping data. Implemented in Infrastructure.</summary>
public interface IProjectRepository
{
    /// <summary>All active projects, ordered by name (for the nav/project switcher).</summary>
    Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Returns the active project's id for the given route code, or null if not found.</summary>
    Task<int?> GetIdByCodeAsync(string code, CancellationToken cancellationToken);
}
