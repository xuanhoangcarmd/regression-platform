namespace RegressionPlatform.API.Contracts;

/// <summary>
/// Request body for creating a compare of two runs. Empty/omitted <c>ApiIds</c> = compare all of the
/// project's requests; otherwise only requests of the selected APIs. <c>BaselineMode</c>: 'Expected'
/// = use Run A's edited (expected) responses when available; omitted / 'Original' = captured only.
/// </summary>
/// <c>IgnoreOrder</c>: omitted / true = order-insensitive (default); false = object-key and array order count.
public sealed record CreateCompareRequest(
    long RunAId,
    long RunBId,
    IReadOnlyList<int>? ApiIds,
    string? BaselineMode = null,
    bool? IgnoreOrder = null
);
