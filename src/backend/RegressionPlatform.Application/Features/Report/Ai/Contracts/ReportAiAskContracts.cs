namespace RegressionPlatform.Application.Features.Report.Ai.Contracts;

/// <summary>
/// The whitelisted, read-only drill-down sources an AI provider may request while answering a
/// question about a report. Everything is scoped to the current report — a provider can only ask
/// for these named sources, never for arbitrary data. Kept small on purpose (token-frugal): the
/// base context is sent up-front, and detail is fetched only when the model asks for it.
/// </summary>
public static class ReportAiSources
{
    /// <summary>Field-level change patterns (with old→new samples) for one API named in the data.</summary>
    public const string ApiDetail = "apiDetail";

    /// <summary>Requests with the highest response time (Run A vs Run B durations).</summary>
    public const string SlowestRequests = "slowestRequests";

    /// <summary>Human-readable catalog shown to the model — the "menu" of sources it may request.</summary>
    public static readonly IReadOnlyList<string> Catalog =
    [
        "apiDetail(apiName): field-level changes (old→new value samples) for one API named in the data.",
        "slowestRequests(): the requests with the highest response time, with Run A vs Run B durations.",
    ];
}

/// <summary>A provider's request for one extra drill-down source (see <see cref="ReportAiSources"/>).</summary>
public sealed record ReportAiNeed(string Source, string? Arg);

/// <summary>
/// A provider's reply to an Ask-AI turn: either a final <see cref="Answer"/> or a <see cref="Need"/>
/// for one more data source. If both are set, the answer wins (the need is ignored).
/// </summary>
public sealed record ReportAiAnswer(string? Answer, ReportAiNeed? Need)
{
    public bool HasAnswer => !string.IsNullOrWhiteSpace(Answer);
}

/// <summary>
/// Input to a provider's <c>AskAsync</c>. <see cref="FetchedDetail"/> is null on the first turn; on
/// the follow-up turn (after a <see cref="ReportAiNeed"/> was fulfilled) it carries the fetched
/// source text and the provider must produce an answer rather than ask for more.
/// </summary>
public sealed record ReportAiAskRequest(
    ReportAiContext BaseContext,
    string Question,
    string? FetchedDetail
);

/// <summary>The final Ask-AI answer returned to the UI (answer text + which provider produced it).</summary>
public sealed record ReportAiAskResponse(string Answer, string Provider);
