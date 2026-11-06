using RegressionPlatform.Application.Features.Runs.Contracts;

namespace RegressionPlatform.Application.Features.Runs;

/// <summary>Data access for a run's expected (edited) responses. Implemented in Infrastructure.</summary>
public interface IExpectedResponseRepository
{
    /// <summary>APIs that have responses in the run, with request + edited counts.</summary>
    Task<IReadOnlyList<ExpectedApiItem>> GetApisAsync(long runId, CancellationToken cancellationToken);

    /// <summary>The run's executed requests of one API (no bodies), ordered by title.</summary>
    Task<IReadOnlyList<ExpectedRequestItem>> GetRequestsAsync(long runId, int apiId, CancellationToken cancellationToken);

    /// <summary>Original + edited bodies for the given requests (only those executed in the run).</summary>
    Task<IReadOnlyList<ExpectedResponseDetail>> GetDetailsAsync(
        long runId,
        IReadOnlyCollection<long> requestIds,
        CancellationToken cancellationToken
    );

    /// <summary>How many of <paramref name="requestIds"/> have a captured response in the run.</summary>
    Task<int> CountResponsesAsync(long runId, IReadOnlyCollection<long> requestIds, CancellationToken cancellationToken);

    /// <summary>
    /// Upserts the edited responses in one transaction (canonicalizes + hashes each body exactly like
    /// a capture). Returns the number of saved rows.
    /// </summary>
    Task<int> SaveAsync(
        long runId,
        IReadOnlyList<ExpectedResponseSaveItem> items,
        string? userName,
        CancellationToken cancellationToken
    );

    /// <summary>Deletes the edited versions (back to the original). Returns the number removed.</summary>
    Task<int> DeleteAsync(long runId, IReadOnlyCollection<long> requestIds, CancellationToken cancellationToken);
}
