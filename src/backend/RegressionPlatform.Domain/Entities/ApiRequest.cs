namespace RegressionPlatform.Domain.Entities;

/// <summary>A reusable, versioned test request for an <see cref="Api"/>. Mirrors the <c>ApiRequest</c> table.</summary>
public class ApiRequest
{
    public long Id { get; set; }
    public int DatasetId { get; set; }
    public int ApiId { get; set; }
    public string Title { get; set; } = string.Empty;
    /// <summary>Optional free-text note about the request (what it covers / where it came from).</summary>
    public string? Description { get; set; }
    public string? HeadersJson { get; set; }
    public string? QueryStringJson { get; set; }
    public string? RequestBody { get; set; }
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
}
