using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RegressionPlatform.Application.Features.Report.Ai;
using RegressionPlatform.Application.Features.Report.Ai.Contracts;
using RegressionPlatform.Application.Features.Report.Contracts;

namespace RegressionPlatform.Infrastructure.Ai.Providers;

/// <summary>
/// DeepSeek AI provider (OpenAI-compatible chat completions). Sends the report context and asks for
/// a strict JSON analysis. Throws if not configured or on a bad response, so the caller can fall back.
/// </summary>
public sealed class DeepSeekProvider(HttpClient httpClient, AiOptions options) : IAiProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly string[] ValidRiskLevels = [
        ReportRiskLevels.Low, ReportRiskLevels.Medium, ReportRiskLevels.High, ReportRiskLevels.Critical,
    ];

    public string Name => AiProviderNames.DeepSeek;

    public async Task<ReportAiAnalysisResult> AnalyzeReportAsync(ReportAiContext context, CancellationToken cancellationToken)
    {
        var settings = options.DeepSeek;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("DeepSeek API key is not configured (AI:DeepSeek:ApiKey).");
        }

        var request = new
        {
            model = settings.Model,
            stream = false,
            temperature = 0.3,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = BuildUserPrompt(context) },
            },
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
        {
            Content = JsonContent.Create(request),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var chat = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions, cancellationToken);
        var content = chat?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("DeepSeek returned an empty response.");
        }

        var parsed = JsonSerializer.Deserialize<LlmAnalysis>(content, JsonOptions)
            ?? throw new InvalidOperationException("DeepSeek response was not valid JSON.");

        return new ReportAiAnalysisResult(
            NonEmpty(parsed.Summary, "AI summary unavailable."),
            parsed.Findings ?? [],
            NonEmpty(parsed.Recommendation, string.Empty),
            NormalizeRisk(parsed.RiskLevel, context.RiskLevel),
            Clamp(parsed.RiskScore),
            parsed.RiskFactors ?? [],
            parsed.Recommendations ?? [],
            parsed.ExampleQuestions ?? [],
            Name
        );
    }

    public async Task<ReportAiAnswer> AskAsync(ReportAiAskRequest request, CancellationToken cancellationToken)
    {
        var settings = options.DeepSeek;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("DeepSeek API key is not configured (AI:DeepSeek:ApiKey).");
        }

        var chatRequest = new
        {
            model = settings.Model,
            stream = false,
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = AskSystemPrompt },
                new { role = "user", content = BuildAskPrompt(request) },
            },
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
        {
            Content = JsonContent.Create(chatRequest),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var chat = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions, cancellationToken);
        var content = chat?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("DeepSeek returned an empty response.");
        }

        var parsed = JsonSerializer.Deserialize<LlmAskResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("DeepSeek response was not valid JSON.");

        // A final answer always wins over a (possibly stray) drill-down request. Only honour a fresh
        // request on the first turn — never after we already handed the model the fetched detail.
        if (!string.IsNullOrWhiteSpace(parsed.Answer))
        {
            return new ReportAiAnswer(parsed.Answer, Need: null);
        }

        if (request.FetchedDetail is null
            && parsed.Need is { Source: { } source }
            && !string.IsNullOrWhiteSpace(source))
        {
            return new ReportAiAnswer(Answer: null, new ReportAiNeed(source, parsed.Need.Arg));
        }

        return new ReportAiAnswer(Answer: null, Need: null);
    }

    private static readonly string AskSystemPrompt =
        "You are a QA/release assistant answering a user's question about ONE specific API regression "
        + "report (Run A = baseline, Run B = current). Use ONLY the provided report data — never invent "
        + "values, and never answer from outside knowledge. "
        + "If the question is not about THIS regression report or its results (e.g. general chit-chat, "
        + "coding help, unrelated topics), do NOT answer it — instead return exactly "
        + "{\"answer\": \"I can only answer questions about this regression report.\"}. "
        + "If you need more detail than the base data to answer, you may request ONE data source by "
        + "returning {\"need\": {\"source\": \"<name>\", \"arg\": \"<value>\"}}. Available sources:\n"
        + "- apiDetail(apiName): field-level changes (old→new value samples) for one API named in the data.\n"
        + "- slowestRequests(): the requests with the highest response time, with Run A vs Run B durations.\n"
        + "Otherwise return {\"answer\": \"<concise answer, citing the actual API names and numbers>\"}. "
        + "Respond with ONLY that JSON object (keys: answer, need) and nothing else.";

    private static string BuildAskPrompt(ReportAiAskRequest request)
    {
        var prompt =
            "User question: " + request.Question
            + "\n\nReport data:\n" + JsonSerializer.Serialize(request.BaseContext, JsonOptions);
        if (request.FetchedDetail is not null)
        {
            prompt +=
                "\n\nRequested detail:\n" + request.FetchedDetail
                + "\n\nAnswer the user's question now using the data above. Do not request any more sources.";
        }
        return prompt;
    }

    private const string SystemPrompt =
        "You are a senior QA/release engineer analyzing the results of an API regression test that "
        + "compares two runs (Run A = baseline, Run B = current). Respond with ONLY a JSON object with "
        + "these keys: summary (string), findings (array of short strings), recommendation (string), "
        + "riskLevel (one of \"Low\", \"Medium\", \"High\", \"Critical\"), riskScore (integer 0-100, higher = riskier), "
        + "riskFactors (array of short strings), recommendations (array of short actionable strings), "
        + "exampleQuestions (array of exactly 3 short questions a user might ask about this report). "
        + "Be concise and specific: reference the actual API names and numbers from the data. Do not invent data.";

    private static string BuildUserPrompt(ReportAiContext context) =>
        "Analyze this regression report and return the JSON described. Data:\n"
        + JsonSerializer.Serialize(context, JsonOptions);

    private static string NonEmpty(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static int Clamp(int? score) => Math.Clamp(score ?? 0, 0, 100);

    private static string NormalizeRisk(string? riskLevel, string fallback) =>
        riskLevel is not null && ValidRiskLevels.Contains(riskLevel, StringComparer.OrdinalIgnoreCase)
            ? ValidRiskLevels.First(level => string.Equals(level, riskLevel, StringComparison.OrdinalIgnoreCase))
            : fallback;

    // ----- DeepSeek (OpenAI-compatible) response + parsed analysis -----

    private sealed class ChatResponse
    {
        public List<Choice>? Choices { get; init; }
    }

    private sealed class Choice
    {
        public ResponseMessage? Message { get; init; }
    }

    private sealed class ResponseMessage
    {
        public string? Content { get; init; }
    }

    private sealed record LlmAnalysis(
        string? Summary,
        string[]? Findings,
        string? Recommendation,
        string? RiskLevel,
        int? RiskScore,
        string[]? RiskFactors,
        string[]? Recommendations,
        string[]? ExampleQuestions
    );

    // ----- Ask-AI parsed reply (either an answer or a request for one drill-down source) -----

    private sealed record LlmAskResponse(string? Answer, LlmNeed? Need);

    private sealed record LlmNeed(string? Source, string? Arg);
}
