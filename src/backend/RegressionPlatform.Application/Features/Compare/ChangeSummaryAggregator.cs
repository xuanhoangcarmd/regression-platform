using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using RegressionPlatform.Application.Features.Compare.Contracts;

namespace RegressionPlatform.Application.Features.Compare;

/// <summary>
/// Pure aggregation of per-request field diffs into distinct change patterns. Array indices in paths
/// are normalized (<c>Monitors[3]</c> → <c>Monitors[*]</c>) so the same field across requests/elements
/// collapses into one pattern with counts + representative samples. No IO — easy to test and to reuse
/// as the input for an AI summary later.
/// </summary>
public static partial class ChangeSummaryAggregator
{
    private const int MaxValueLength = 200;

    /// <summary>
    /// Groups raw diffs by API, then aggregates each API's diffs into distinct patterns
    /// (ordered by occurrences desc). Keeps multi-API compares distinguishable.
    /// </summary>
    public static IReadOnlyList<CompareApiChangeGroup> Aggregate(IReadOnlyList<CompareRequestDiff> diffs, int maxSamples)
    {
        return diffs
            .GroupBy(diff => diff.ApiId)
            .Select(apiGroup =>
            {
                var first = apiGroup.First();
                var patterns = AggregatePatterns(apiGroup, maxSamples);
                var requestCount = apiGroup.Select(d => d.RequestId).Distinct().Count();
                return new CompareApiChangeGroup(
                    first.ApiId, first.ApiName, first.Method, first.Endpoint,
                    requestCount, patterns.Count, patterns
                );
            })
            .OrderBy(group => group.ApiName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Endpoint, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Aggregates one API's diffs into distinct patterns, ordered by occurrences (desc).</summary>
    private static IReadOnlyList<CompareChangePattern> AggregatePatterns(IEnumerable<CompareRequestDiff> diffs, int maxSamples)
    {
        var groups = new Dictionary<(string ChangeType, string Path), PatternAccumulator>();

        foreach (var diff in diffs)
        {
            JsonArray? entries;
            try
            {
                entries = JsonNode.Parse(diff.DiffJson) as JsonArray;
            }
            catch (JsonException)
            {
                continue;
            }
            if (entries is null)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                if (entry is null)
                {
                    continue;
                }
                var changeType = entry["changeType"]?.GetValue<string>() ?? "Unknown";
                var path = NormalizePath(entry["path"]?.GetValue<string>() ?? "$");

                var key = (changeType, path);
                if (!groups.TryGetValue(key, out var accumulator))
                {
                    accumulator = new PatternAccumulator();
                    groups[key] = accumulator;
                }
                accumulator.Add(diff.RequestId, ValueText(entry["old"]), ValueText(entry["new"]));
            }
        }

        return groups
            .Select(group => group.Value.ToPattern(group.Key.ChangeType, group.Key.Path, maxSamples))
            .OrderByDescending(pattern => pattern.Occurrences)
            .ThenBy(pattern => pattern.Path, StringComparer.Ordinal)
            .ToList();
    }

    private static string NormalizePath(string path) => ArrayIndexPattern().Replace(path, "[*]");

    /// <summary>Compact text for a value: raw string for JSON strings, else compact JSON; truncated.</summary>
    private static string? ValueText(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }
        var text = node is JsonValue value && value.TryGetValue<string>(out var s) ? s : node.ToJsonString();
        return text.Length <= MaxValueLength ? text : text[..MaxValueLength] + "…";
    }

    [GeneratedRegex(@"\[\d+\]")]
    private static partial Regex ArrayIndexPattern();

    private sealed class PatternAccumulator
    {
        private readonly HashSet<long> requestIds = [];
        private readonly HashSet<(string? Old, string? New)> seen = [];
        private readonly List<(string? Old, string? New)> distinct = [];
        private int occurrences;

        public void Add(long requestId, string? old, string? @new)
        {
            occurrences++;
            requestIds.Add(requestId);
            if (seen.Add((old, @new)))
            {
                distinct.Add((old, @new));
            }
        }

        public CompareChangePattern ToPattern(string changeType, string path, int maxSamples)
        {
            var samples = distinct.Take(maxSamples).Select(t => new CompareChangeSample(t.Old, t.New)).ToList();
            return new CompareChangePattern(changeType, path, occurrences, requestIds.Count, distinct.Count, samples);
        }
    }
}
