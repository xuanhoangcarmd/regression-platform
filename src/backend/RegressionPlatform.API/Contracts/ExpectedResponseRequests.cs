namespace RegressionPlatform.API.Contracts;

/// <summary>Request ids of a run (details / restore).</summary>
public sealed record ExpectedRequestIdsBody(IReadOnlyList<long> RequestIds);

/// <summary>One edited response to save. <c>StatusCode</c> null = keep the original status code.</summary>
public sealed record ExpectedResponseItemBody(long RequestId, int? StatusCode, string ResponseJson, string? Reason);

/// <summary>Batch of edited responses saved in one transaction.</summary>
public sealed record SaveExpectedResponsesBody(IReadOnlyList<ExpectedResponseItemBody> Items);
