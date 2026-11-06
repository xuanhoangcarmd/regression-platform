using RegressionPlatform.Application.Features.DataLibrary.Contracts;
using RegressionPlatform.Shared.Pagination;

namespace RegressionPlatform.Application.Features.DataLibrary;

/// <summary>Read-side data access for the Data Library (search + content). Implemented in Infrastructure.</summary>
public interface IDataLibraryRepository
{
    /// <summary>Search vehicle profiles by VIN / YMME / MIL / DTC code, paged.</summary>
    Task<PagedResult<VehicleProfileListItem>> SearchProfilesAsync(
        VehicleProfileSearchParameters parameters,
        CancellationToken cancellationToken
    );

    /// <summary>Distinct YMME options (faceted by the given selections) for the cascading filter.</summary>
    Task<YmmeOptionsResponse> GetYmmeOptionsAsync(
        string? year,
        string? make,
        string? model,
        string? engine,
        CancellationToken cancellationToken
    );

    /// <summary>Full detail of one profile (scalars + DTCs + monitors + freeze frames + payload/buffer metadata).</summary>
    Task<VehicleProfileDetailResponse?> GetProfileByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>One payload's content (decompressed → base64) + metadata, or null if not found.</summary>
    Task<PayloadContentResponse?> GetPayloadContentAsync(long id, CancellationToken cancellationToken);

    /// <summary>One raw buffer's content (decompressed → base64) + metadata, or null if not found.</summary>
    Task<RawBufferContentResponse?> GetRawBufferContentAsync(long id, CancellationToken cancellationToken);
}
