namespace RegressionPlatform.Domain.Entities;

/// <summary>
/// One captured response from executing a request within a run. Mirrors the <c>RunResponse</c>
/// table. <see cref="ResponseJson"/> holds the response as returned (compact, ORIGINAL key order;
/// rows captured before 2026-10-19 hold the key-sorted canonical form). <see cref="ResponseHash"/> is
/// SHA-256 of the key-sorted canonical body (order-insensitive identity); <see cref="ResponseOrderedHash"/>
/// is SHA-256 of the original-order body (strict-order compares). Encryption is planned.
/// </summary>
public class RunResponse
{
    public long Id { get; set; }
    public long RunId { get; set; }
    public long RequestId { get; set; }
    public int? StatusCode { get; set; }
    public int? DurationMs { get; set; }
    public string? ResponseHash { get; set; }
    public string? ResponseOrderedHash { get; set; }
    public string? ResponseJson { get; set; }

    /// <summary>JSON object of response (and content) header name → value; null when the request errored.</summary>
    public string? ResponseHeaders { get; set; }
    public int? ResponseSize { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedDate { get; set; }
}
