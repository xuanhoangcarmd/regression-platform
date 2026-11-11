using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RegressionPlatform.Application.Abstractions;
using RegressionPlatform.Application.Features.ProjectConfiguration;
using RegressionPlatform.Application.Features.Runs;
using RegressionPlatform.Application.Features.Runs.Contracts;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Infrastructure.Services;

/// <summary>
/// Executes a run: for each target request, calls the real API using the environment's Base URL +
/// headers (from Configurations), then stores the response via the canonical → hash pipeline.
/// Run lifecycle: the run arrives Queued; this claims it (Queued → Running, atomic — a run cancelled
/// while waiting is skipped) and finishes it Completed (or Failed on a fatal error).
/// </summary>
public sealed class RunExecutor(
    IRunExecutionRepository executionRepository,
    IProjectConfigurationRepository configurationRepository,
    ISecretProtector secretProtector,
    IHttpClientFactory httpClientFactory,
    IRunProgressTracker progressTracker,
    IRunCancellationRegistry cancellationRegistry,
    ILogger<RunExecutor> logger
) : IRunRequestExecutor
{
    public const string HttpClientName = "RunExecutor";

    /// <summary>A successful response slower than this (ms) counts as a Warning.</summary>
    private const int WarningThresholdMs = 2000;

    private static readonly HashSet<string> BodylessMethods = new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD" };

    public async Task ExecuteAsync(long runId, CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();
        // Per-run cancellation token: trips on app shutdown (stoppingToken) OR a user Stop request.
        var cancellationToken = cancellationRegistry.Register(runId, stoppingToken);
        try
        {
            var run = await executionRepository.GetExecutionInfoAsync(runId, cancellationToken);
            if (run is null)
            {
                logger.LogWarning("Run {RunId} disappeared before execution.", runId);
                return;
            }

            // Claim the run (Queued → Running, atomic). 0 rows = it was cancelled (or otherwise
            // finalized) while waiting in the queue — nothing to execute.
            if (!await executionRepository.TryMarkRunningAsync(runId, DateTime.UtcNow, cancellationToken))
            {
                logger.LogInformation("Run {RunId} is no longer Queued (cancelled while waiting); skipping.", runId);
                return;
            }

            // Resolve the environment configuration (Base URL + headers, secrets decrypted).
            var configurations = await configurationRepository.GetConfigurationsAsync(run.ProjectId, cancellationToken);
            var configuration = configurations.FirstOrDefault(c =>
                string.Equals(c.Environment, run.Environment, StringComparison.OrdinalIgnoreCase)
            );

            if (configuration is null || string.IsNullOrWhiteSpace(configuration.BaseUrl))
            {
                logger.LogWarning(
                    "Run {RunId} has no Base URL configured for environment {Environment}; marking Failed.",
                    runId,
                    run.Environment
                );
                await executionRepository.CompleteRunAsync(runId, "Failed", DateTime.UtcNow, stopwatch.ElapsedMilliseconds, 0, 0, 0, 0, cancellationToken);
                return;
            }

            var baseUrl = configuration.BaseUrl.TrimEnd('/');
            var environmentHeaders = BuildEnvironmentHeaders(configuration);

            var targetApiIds = await executionRepository.GetTargetApiIdsAsync(runId, cancellationToken);
            var requests = await executionRepository.GetRunnableRequestsAsync(run.DatasetId, targetApiIds, cancellationToken);

            // Optional cap: at most N requests per API (requests come ordered by id, so we take the first N).
            if (run.RequestLimitPerApi is int limit && limit > 0)
            {
                requests = requests
                    .GroupBy(request => request.ApiId)
                    .SelectMany(group => group.Take(limit))
                    .ToList();
            }

            // Track live progress in memory (not persisted).
            progressTracker.Start(runId, requests.Count);

            var client = httpClientFactory.CreateClient(HttpClientName);
            var counts = new HealthCounts();

            // Executes ONE request end-to-end: call → store → progress → classify.
            // Counters use Interlocked so this is safe to run concurrently (parallel modes).
            async Task ProcessAsync(RunnableRequest request)
            {
                var response = await ExecuteRequestAsync(client, runId, baseUrl, environmentHeaders, request, cancellationToken);
                await executionRepository.InsertResponseAsync(response, cancellationToken);
                progressTracker.Increment(runId);

                // Classify for execution-health metrics: failed → warning (slow) → pass.
                if (IsFailed(response))
                {
                    Interlocked.Increment(ref counts.Failed);
                }
                else if (response.DurationMs is int durationMs && durationMs > WarningThresholdMs)
                {
                    Interlocked.Increment(ref counts.Warning);
                }
                else
                {
                    Interlocked.Increment(ref counts.Pass);
                }
            }

            // Concurrency knobs (guarded ≥ 1). 1 × 1 keeps the original sequential path unchanged.
            var maxApis = Math.Max(1, run.MaxParallelApis);
            var maxRequestsPerApi = Math.Max(1, run.MaxParallelRequestsPerApi);

            if (maxApis == 1 && maxRequestsPerApi == 1)
            {
                foreach (var request in requests)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ProcessAsync(request);
                }
            }
            else
            {
                // Nested parallelism: up to `maxApis` API groups at once; within each group up to
                // `maxRequestsPerApi` requests at once. Effective concurrency = maxApis × maxRequestsPerApi.
                var apiGroups = requests.GroupBy(request => request.ApiId).ToList();
                await Parallel.ForEachAsync(
                    apiGroups,
                    new ParallelOptions { MaxDegreeOfParallelism = maxApis, CancellationToken = cancellationToken },
                    async (group, _) =>
                        await Parallel.ForEachAsync(
                            group,
                            new ParallelOptions { MaxDegreeOfParallelism = maxRequestsPerApi, CancellationToken = cancellationToken },
                            async (request, _) => await ProcessAsync(request)
                        )
                );
            }

            stopwatch.Stop();
            await executionRepository.CompleteRunAsync(
                runId, "Completed", DateTime.UtcNow, stopwatch.ElapsedMilliseconds, requests.Count, counts.Pass, counts.Warning, counts.Failed, cancellationToken);
            logger.LogInformation(
                "Run {RunId} completed: {Total} request(s) (pass {Pass}, warning {Warning}, failed {Failed}) in {Elapsed}ms.",
                runId, requests.Count, counts.Pass, counts.Warning, counts.Failed, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw; // app shutdown — leave the run as Running; do not mark Failed/Cancelled.
        }
        catch (OperationCanceledException)
        {
            // User Stop — the stop request already set the run to 'Cancelled'; just stop quietly.
            logger.LogInformation("Run {RunId} was cancelled by a stop request.", runId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Run {RunId} failed during execution.", runId);
            await executionRepository.CompleteRunAsync(runId, "Failed", DateTime.UtcNow, stopwatch.ElapsedMilliseconds, 0, 0, 0, 0, CancellationToken.None);
        }
        finally
        {
            // Always free the per-run cancellation source + in-memory progress (completed/failed/cancelled).
            cancellationRegistry.Remove(runId);
            progressTracker.Remove(runId);
        }
    }

    /// <summary>
    /// Re-runs a single request of an existing run: resolves the environment config, calls the API again,
    /// replaces that request's stored response, and recomputes the run's health counts so the summary/list
    /// stay consistent. (Ignore/extract rules are applied at compare time, not here.)
    /// </summary>
    public async Task<bool> RerunRequestAsync(long runId, long requestId, CancellationToken cancellationToken)
    {
        var run = await executionRepository.GetExecutionInfoAsync(runId, cancellationToken);
        if (run is null)
        {
            return false;
        }

        var configurations = await configurationRepository.GetConfigurationsAsync(run.ProjectId, cancellationToken);
        var configuration = configurations.FirstOrDefault(c =>
            string.Equals(c.Environment, run.Environment, StringComparison.OrdinalIgnoreCase)
        );
        if (configuration is null || string.IsNullOrWhiteSpace(configuration.BaseUrl))
        {
            logger.LogWarning("Re-run of request {RequestId} in run {RunId} skipped: no Base URL for {Environment}.", requestId, runId, run.Environment);
            return false;
        }

        var request = await executionRepository.GetRunnableRequestAsync(requestId, cancellationToken);
        if (request is null)
        {
            return false;
        }

        var baseUrl = configuration.BaseUrl.TrimEnd('/');
        var environmentHeaders = BuildEnvironmentHeaders(configuration);

        var client = httpClientFactory.CreateClient(HttpClientName);
        var response = await ExecuteRequestAsync(client, runId, baseUrl, environmentHeaders, request, cancellationToken);

        await executionRepository.ReplaceResponseAsync(response, cancellationToken);
        await executionRepository.RecalculateRunCountsAsync(runId, cancellationToken);
        logger.LogInformation("Re-ran request {RequestId} in run {RunId}: status {StatusCode}.", requestId, runId, response.StatusCode);
        return true;
    }

    /// <summary>Mutable health tallies; incremented with Interlocked so parallel workers stay correct.</summary>
    private sealed class HealthCounts
    {
        public int Pass;
        public int Warning;
        public int Failed;
    }

    /// <summary>A request "failed" if no response (network error) or the API returned HTTP &gt;= 400.</summary>
    private static bool IsFailed(DomainEntities.RunResponse response) =>
        response.ErrorMessage is not null || response.StatusCode is null || response.StatusCode >= 400;

    private async Task<DomainEntities.RunResponse> ExecuteRequestAsync(
        HttpClient client,
        long runId,
        string baseUrl,
        IReadOnlyDictionary<string, string> environmentHeaders,
        RunnableRequest request,
        CancellationToken cancellationToken
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var url = BuildUrl(baseUrl, request.Endpoint, request.QueryStringJson);
            using var message = new HttpRequestMessage(new HttpMethod(request.Method), url);

            ApplyHeaders(message, environmentHeaders);
            ApplyHeaders(message, ParseStringMap(request.HeadersJson));

            if (!BodylessMethods.Contains(request.Method) && !string.IsNullOrEmpty(request.RequestBody))
            {
                message.Content = new StringContent(request.RequestBody, Encoding.UTF8, "application/json");
            }

            using var response = await client.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            // Store the FULL response (formatted, all fields kept) and hash the SAME full canonical body.
            // The hash is a pure identity check for the compare engine's fast path; ignore/extract rules
            // are applied fresh AT COMPARE TIME (deep path). Baking capture-time ignore rules into this
            // hash made the fast path wrongly classify Unchanged when a rule was later disabled/removed
            // (two bodies that differ only in a then-ignored field hashed equal).
            //
            // The body is stored in its ORIGINAL key order (as the API returned it). ResponseHash stays the
            // key-sorted canonical hash (order-insensitive — the default compare); ResponseOrderedHash is
            // the original-order hash used by strict-order compares.
            var canonical = ResponseCanonicalizer.Process(body, []);
            var original = ResponseCanonicalizer.ProcessOrdered(body, []);
            return new DomainEntities.RunResponse
            {
                RunId = runId,
                RequestId = request.RequestId,
                StatusCode = (int)response.StatusCode,
                DurationMs = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue),
                ResponseHash = canonical.Hash,
                ResponseOrderedHash = original.Hash,
                ResponseJson = original.Canonical,
                ResponseHeaders = SerializeHeaders(response),
                ResponseSize = Encoding.UTF8.GetByteCount(original.Canonical),
                ErrorMessage = null,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Network/timeout/invalid request → record the failure for this request and continue the run.
            stopwatch.Stop();
            return new DomainEntities.RunResponse
            {
                RunId = runId,
                RequestId = request.RequestId,
                StatusCode = null,
                DurationMs = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue),
                ResponseHash = null,
                ResponseJson = null,
                ResponseSize = null,
                ErrorMessage = Truncate(ex.Message, 2000),
            };
        }
    }

    private Dictionary<string, string> BuildEnvironmentHeaders(DomainEntities.ProjectConfiguration configuration)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in configuration.Headers)
        {
            if (string.IsNullOrWhiteSpace(header.HeaderKey))
            {
                continue;
            }

            if (header.IsSecret)
            {
                headers[header.HeaderKey] =
                    header.SecretValueEncrypted is { Length: > 0 } ? secretProtector.Decrypt(header.SecretValueEncrypted) : string.Empty;
            }
            else
            {
                headers[header.HeaderKey] = header.HeaderValue ?? string.Empty;
            }
        }
        return headers;
    }

    private static void ApplyHeaders(HttpRequestMessage message, IReadOnlyDictionary<string, string> headers)
    {
        foreach (var (key, value) in headers)
        {
            // Content headers (e.g. Content-Type) are rejected here and handled by the body content; that's fine.
            message.Headers.TryAddWithoutValidation(key, value);
        }
    }

    // Path template placeholders like {vin} or {odometer} (no slash/brace inside).
    private static readonly Regex PathParameterPattern = new(@"\{([^{}/]+)\}", RegexOptions.Compiled);

    private static string BuildUrl(string baseUrl, string endpoint, string? queryStringJson)
    {
        var path = string.IsNullOrEmpty(endpoint) ? string.Empty : (endpoint.StartsWith('/') ? endpoint : "/" + endpoint);

        var query = ParseStringMap(queryStringJson);

        // Substitute {placeholder} path parameters from the query map (case-insensitive). A field that
        // fills a placeholder is CONSUMED, so it does not also become a query-string parameter. This
        // lets one imported JSON carry both path params (vin, odometer) and query params (region, …).
        path = SubstitutePathParameters(path, query);

        var url = baseUrl + path;
        if (query.Count == 0)
        {
            return url;
        }

        var queryString = string.Join('&', query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        return url + (url.Contains('?') ? '&' : '?') + queryString;
    }

    /// <summary>
    /// Replaces each <c>{name}</c> in the path with the matching field from <paramref name="query"/>
    /// (URL-encoded), removing consumed fields. A placeholder whose field is missing, JSON null or empty
    /// becomes the literal <c>null</c> (e.g. <c>?category=null</c>) instead of being sent as <c>{category}</c>.
    /// </summary>
    private static string SubstitutePathParameters(string path, Dictionary<string, string> query)
    {
        if (path.IndexOf('{') < 0)
        {
            return path;
        }

        return PathParameterPattern.Replace(path, match =>
        {
            var name = match.Groups[1].Value.Trim();
            if (query.Remove(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return Uri.EscapeDataString(value);
            }
            return MissingParameterValue; // no value for this placeholder — send "null"
        });
    }

    /// <summary>Sent in place of a URL placeholder that has no value in the request.</summary>
    private const string MissingParameterValue = "null";

    /// <summary>Parses a JSON object of string-ish values into a map; returns empty for null/invalid input.</summary>
    private static Dictionary<string, string> ParseStringMap(string? json)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            return map;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return map;
            }
            foreach (var property in document.RootElement.EnumerateObject())
            {
                map[property.Name] =
                    property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString() ?? string.Empty
                        : property.Value.GetRawText();
            }
        }
        catch (JsonException)
        {
            // Not a JSON object — ignore (no headers/query applied).
        }
        return map;
    }

    /// <summary>Serializes the response (and content) headers as a JSON object of name → joined value(s).</summary>
    private static string SerializeHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers.Concat(response.Content.Headers))
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }
        return JsonSerializer.Serialize(headers);
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
