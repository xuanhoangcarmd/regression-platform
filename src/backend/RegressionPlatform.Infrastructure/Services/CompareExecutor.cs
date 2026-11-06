using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RegressionPlatform.Application.Features.Compare;
using RegressionPlatform.Application.Features.Compare.Contracts;
using DomainEntities = RegressionPlatform.Domain.Entities;

namespace RegressionPlatform.Infrastructure.Services;

/// <summary>
/// The Compare Engine. Matches Run A vs Run B responses by RequestId, classifies each
/// (Unchanged / Changed / Added / Removed / Failed), deep-diffs the changed ones, then persists the
/// summary + per-request details. Job lifecycle: marks Running, updates Progress, finishes
/// Completed (or Failed on a fatal error).
///
/// Classification (both present): Failed if either side errored; else Unchanged when the response
/// hash AND status code match; otherwise Changed (status code is compared because the hash covers
/// the canonical body only).
///
/// Baseline mode 'Expected': each run's hash / status / body come from its edited (expected) response
/// when one exists (else the captured one). The classification rules are identical in both modes.
/// </summary>
public sealed class CompareExecutor(
    ICompareExecutionRepository repository,
    ICompareCancellationRegistry cancellationRegistry,
    ILogger<CompareExecutor> logger
)
{
    /// <summary>Deep-compare requests whose bodies are loaded per round trip.</summary>
    private const int DeepBatchSize = 500;

    /// <summary>CPU-bound canonicalize + diff runs in parallel, leaving one core for the API.</summary>
    private static readonly int DiffParallelism = Math.Max(1, Environment.ProcessorCount - 1);

    public async Task ExecuteAsync(long jobId, CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();
        // Register a per-job token (linked to app shutdown) so a Stop request can abort this run.
        var cancellationToken = cancellationRegistry.Register(jobId, stoppingToken);
        try
        {
            var info = await repository.GetExecutionInfoAsync(jobId, cancellationToken);
            if (info is null)
            {
                logger.LogWarning("Compare job {JobId} disappeared before execution.", jobId);
                return;
            }

            // Guarded Running (only if still 'Running'). A Stop that flipped it to Cancelled while it
            // waited in the queue makes this a no-op → skip execution instead of resurrecting it.
            if (!await repository.MarkRunningAsync(jobId, DateTime.UtcNow, cancellationToken))
            {
                logger.LogInformation("Compare job {JobId} was cancelled before it started; skipping.", jobId);
                return;
            }

            var useExpected = string.Equals(info.BaselineMode, CompareBaselineModes.Expected, StringComparison.Ordinal);
            var apiIds = await repository.GetScopeApiIdsAsync(jobId, cancellationToken);
            // Ignore rules are re-applied to the stored canonical bodies at compare time (deep path),
            // so rules added after the runs were captured still take effect.
            var ignorePaths = await repository.GetIgnorePathsAsync(info.ProjectId, cancellationToken);
            // Extract-root rules lift a subtree (e.g. $.data) to the response root before comparison,
            // so an envelope-wrapped body compares cleanly against an un-wrapped one. Like ignore rules,
            // they are applied to the stored bodies here (deep path) — never at capture time.
            var extractPaths = await repository.GetExtractRootPathsAsync(info.ProjectId, cancellationToken);
            // TrimAfter rules cut string values at a delimiter (e.g. a signed URL's "?…") — deep path only.
            var trimRules = await repository.GetTrimRulesAsync(info.ProjectId, cancellationToken);
            var responsesA = await repository.GetResponseSummariesAsync(info.RunAId, apiIds, useExpected, cancellationToken);
            var responsesB = await repository.GetResponseSummariesAsync(info.RunBId, apiIds, useExpected, cancellationToken);

            var mapA = responsesA.ToDictionary(response => response.RequestId);
            var mapB = responsesB.ToDictionary(response => response.RequestId);
            var requestIds = mapA.Keys.Union(mapB.Keys).ToList();

            var details = new DomainEntities.CompareDetail[requestIds.Count];
            var deep = new List<DeepItem>();
            int unchanged = 0, changed = 0, added = 0, removed = 0, failed = 0;

            // Pass 1 — classify everything decidable from the summaries (no bodies loaded).
            for (var index = 0; index < requestIds.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var requestId = requestIds[index];
                var hasA = mapA.TryGetValue(requestId, out var a);
                var hasB = mapB.TryGetValue(requestId, out var b);

                string result;
                if (hasA && !hasB)
                {
                    result = CompareResultTypes.Removed;
                    removed++;
                }
                else if (!hasA && hasB)
                {
                    result = CompareResultTypes.Added;
                    added++;
                }
                else if (a!.ErrorMessage is not null || b!.ErrorMessage is not null)
                {
                    result = CompareResultTypes.Failed;
                    failed++;
                }
                else if (string.Equals(HashOf(a, info.IgnoreOrder), HashOf(b, info.IgnoreOrder), StringComparison.Ordinal) && a.StatusCode == b.StatusCode)
                {
                    // Fast path: stored responses are byte-identical (ignored fields, if any, match too).
                    result = CompareResultTypes.Unchanged;
                    unchanged++;
                }
                else
                {
                    // Deep path (pass 2): needs both bodies.
                    deep.Add(new DeepItem(index, requestId, a, b));
                    continue;
                }

                details[index] = NewDetail(requestId, result, diffJson: null, isEdited: (hasA && a!.IsEdited) || (hasB && b!.IsEdited));
            }

            var progress = new ProgressReporter(repository, jobId, requestIds.Count);
            await progress.ReportAsync(requestIds.Count - deep.Count, cancellationToken);

            // Per-API rule lists, resolved once instead of per request.
            var ignoreByApi = new Dictionary<int, List<string>>();
            var extractByApi = new Dictionary<int, List<string>>();
            var trimByApi = new Dictionary<int, List<ResponseCanonicalizer.ValueTrim>>();
            foreach (var apiId in deep.Select(item => item.A.ApiId).Distinct())
            {
                ignoreByApi[apiId] = ignorePaths.Where(rule => rule.AppliesTo(apiId)).Select(rule => rule.JsonPath).ToList();
                extractByApi[apiId] = extractPaths.Where(rule => rule.AppliesTo(apiId)).Select(rule => rule.JsonPath).ToList();
                trimByApi[apiId] = trimRules
                    .Where(rule => rule.AppliesTo(apiId))
                    .Select(rule => new ResponseCanonicalizer.ValueTrim(rule.JsonPath, rule.Delimiter))
                    .ToList();
            }

            // Pass 2 — deep compare in batches: one round trip per batch, CPU work in parallel.
            var processed = requestIds.Count - deep.Count;
            foreach (var batch in deep.Chunk(DeepBatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchIds = batch.Select(item => item.RequestId).ToList();
                var bodies = await repository.GetResponseBodiesAsync(info.RunAId, info.RunBId, batchIds, useExpected, cancellationToken);

                var outcomes = new (string Result, string? DiffJson)[batch.Length];
                Parallel.For(
                    0,
                    batch.Length,
                    new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = DiffParallelism },
                    k =>
                    {
                        var item = batch[k];
                        var (jsonA, jsonB) = bodies.TryGetValue(item.RequestId, out var pair) ? pair : (null, null);
                        outcomes[k] = DeepCompare(
                            item, jsonA, jsonB, ignoreByApi[item.A.ApiId], extractByApi[item.A.ApiId], trimByApi[item.A.ApiId], info.IgnoreOrder);
                    }
                );

                for (var k = 0; k < batch.Length; k++)
                {
                    var item = batch[k];
                    var (result, diffJson) = outcomes[k];
                    if (result == CompareResultTypes.Unchanged)
                    {
                        unchanged++;
                    }
                    else
                    {
                        changed++;
                    }
                    details[item.Index] = NewDetail(item.RequestId, result, diffJson, item.A.IsEdited || item.B.IsEdited);
                }

                processed += batch.Length;
                await progress.ReportAsync(processed, cancellationToken);
            }

            var resultEntity = new DomainEntities.CompareResult
            {
                RunAId = info.RunAId,
                RunBId = info.RunBId,
                TotalRequest = requestIds.Count,
                UnchangedCount = unchanged,
                ChangedCount = changed,
                FailedCount = failed,
                AddedCount = added,
                RemovedCount = removed,
                ModifiedCount = 0,
            };

            var resultId = await repository.SaveResultAsync(resultEntity, details, cancellationToken);
            await repository.CompleteJobAsync(jobId, resultId, DateTime.UtcNow, cancellationToken);

            stopwatch.Stop();
            logger.LogInformation(
                "Compare job {JobId} completed ({Mode}): {Total} request(s) (unchanged {Unchanged}, changed {Changed}, added {Added}, removed {Removed}, failed {Failed}; deep {Deep}, edited {Edited}) in {Elapsed}ms.",
                jobId, info.BaselineMode, requestIds.Count, unchanged, changed, added, removed, failed, deep.Count,
                details.Count(detail => detail.IsEdited), stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            // Either a user Stop (the Stop handler already marked it Cancelled) or app shutdown. In
            // both cases leave the job's status as-is and don't mark it Failed. Swallowed so the
            // background worker loop stays quiet (it stops on the queue's own shutdown signal).
            logger.LogInformation("Compare job {JobId} execution was cancelled.", jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Compare job {JobId} failed during execution.", jobId);
            await repository.MarkJobFailedAsync(jobId, DateTime.UtcNow, CancellationToken.None);
        }
        finally
        {
            cancellationRegistry.Remove(jobId);
        }
    }

    /// <summary>
    /// Deep path: re-apply current ignore rules to both stored canonical bodies, then re-compare. If
    /// only ignored fields differ (and status matches) → Unchanged; otherwise Changed, with the ignored
    /// paths excluded from the diff. Unwraps first (if any extract-root rule applies); no applicable
    /// rule → the bodies pass through unchanged. <paramref name="ignoreOrder"/>: key-sorted canonical
    /// bodies + identity-matched arrays (default); otherwise original order, order-sensitive diff.
    /// Pure / thread-safe.
    /// </summary>
    private static (string Result, string? DiffJson) DeepCompare(
        DeepItem item,
        string? jsonA,
        string? jsonB,
        IReadOnlyList<string> applicable,
        IReadOnlyList<string> applicableExtract,
        IReadOnlyList<ResponseCanonicalizer.ValueTrim> trims,
        bool ignoreOrder
    )
    {
        var bodyA = applicableExtract.Count == 0 ? jsonA ?? string.Empty : ResponseCanonicalizer.ExtractRoot(jsonA ?? string.Empty, applicableExtract);
        var bodyB = applicableExtract.Count == 0 ? jsonB ?? string.Empty : ResponseCanonicalizer.ExtractRoot(jsonB ?? string.Empty, applicableExtract);
        var canonicalA = ignoreOrder ? ResponseCanonicalizer.Process(bodyA, applicable, trims) : ResponseCanonicalizer.ProcessOrdered(bodyA, applicable, trims);
        var canonicalB = ignoreOrder ? ResponseCanonicalizer.Process(bodyB, applicable, trims) : ResponseCanonicalizer.ProcessOrdered(bodyB, applicable, trims);

        if (string.Equals(canonicalA.Hash, canonicalB.Hash, StringComparison.Ordinal) && item.A.StatusCode == item.B.StatusCode)
        {
            return (CompareResultTypes.Unchanged, null);
        }
        return (CompareResultTypes.Changed, JsonDiffer.Diff(canonicalA.Canonical, canonicalB.Canonical, strictOrder: !ignoreOrder));
    }

    /// <summary>The identity hash for the fast path: key-sorted (ignore order) or original-order (strict).</summary>
    private static string? HashOf(CompareResponseSummary summary, bool ignoreOrder) =>
        ignoreOrder ? summary.ResponseHash : summary.OrderedHash;

    private static DomainEntities.CompareDetail NewDetail(long requestId, string result, string? diffJson, bool isEdited) =>
        new()
        {
            RequestId = requestId,
            Result = result,
            DiffJson = diffJson,
            IsEdited = isEdited,
        };

    /// <summary>A request that needs a body-level compare (index = its slot in the details array).</summary>
    private sealed record DeepItem(int Index, long RequestId, CompareResponseSummary A, CompareResponseSummary B);

    /// <summary>Writes Progress only when the whole percent increases (at most ~100 updates per job).</summary>
    private sealed class ProgressReporter(ICompareExecutionRepository repository, long jobId, int total)
    {
        private int _lastPercent;

        public async Task ReportAsync(int processed, CancellationToken cancellationToken)
        {
            if (total <= 0)
            {
                return;
            }
            var percent = (int)((double)processed / total * 100);
            if (percent > _lastPercent)
            {
                _lastPercent = percent;
                await repository.UpdateProgressAsync(jobId, percent, cancellationToken);
            }
        }
    }
}
