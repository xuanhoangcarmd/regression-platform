using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetReportPerformanceStats;

public sealed class GetReportPerformanceStatsQueryHandler(
    IReportRepository repository,
    IProjectRepository projectRepository
) : IRequestHandler<GetReportPerformanceStatsQuery, Result<ReportPerformanceStatsResponse>>
{
    /// <summary>Target number of histogram bins over the combined duration range.</summary>
    private const int TargetBins = 20;

    public async Task<Result<ReportPerformanceStatsResponse>> Handle(
        GetReportPerformanceStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<ReportPerformanceStatsResponse>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var report = await repository.GetByIdAsync(request.ReportId, cancellationToken);
        if (report is null || report.ProjectId != projectId.Value)
        {
            return Result<ReportPerformanceStatsResponse>.NotFound(
                new Error("Report.NotFound", $"Report {request.ReportId} does not exist in the project.")
            );
        }

        var rows = await repository.GetPerformanceRowsAsync(report.RunAId, report.RunBId, cancellationToken);

        var durationsA = rows.Where(r => r.DurationMsA.HasValue).Select(r => r.DurationMsA!.Value).OrderBy(v => v).ToList();
        var durationsB = rows.Where(r => r.DurationMsB.HasValue).Select(r => r.DurationMsB!.Value).OrderBy(v => v).ToList();

        var histogram = BuildHistogram(durationsA, durationsB);
        var perApi = BuildPerApi(rows);

        return Result<ReportPerformanceStatsResponse>.Success(
            new ReportPerformanceStatsResponse(report.RunATitle, report.RunBTitle, histogram, perApi)
        );
    }

    /// <summary>Per-API p95 / max / avg-size / avg (avg backs the Δ% column on the client).</summary>
    private static IReadOnlyList<ReportPerfApiStat> BuildPerApi(IReadOnlyList<ReportPerfRow> rows)
    {
        return rows
            .GroupBy(r => r.ApiId)
            .Select(group =>
            {
                var durA = group.Where(r => r.DurationMsA.HasValue).Select(r => r.DurationMsA!.Value).OrderBy(v => v).ToList();
                var durB = group.Where(r => r.DurationMsB.HasValue).Select(r => r.DurationMsB!.Value).OrderBy(v => v).ToList();
                var sizeA = group.Where(r => r.SizeA.HasValue).Select(r => r.SizeA!.Value).ToList();
                var sizeB = group.Where(r => r.SizeB.HasValue).Select(r => r.SizeB!.Value).ToList();
                var first = group.First();
                return new ReportPerfApiStat(
                    first.ApiId, first.ApiName, first.Method,
                    P95MsA: Percentile(durA, 0.95), P95MsB: Percentile(durB, 0.95),
                    MaxMsA: durA.Count > 0 ? durA[^1] : null, MaxMsB: durB.Count > 0 ? durB[^1] : null,
                    MinMsA: durA.Count > 0 ? durA[0] : null, MinMsB: durB.Count > 0 ? durB[0] : null,
                    AvgSizeA: AverageLong(sizeA), AvgSizeB: AverageLong(sizeB),
                    AvgMsA: Average(durA), AvgMsB: Average(durB)
                );
            })
            .OrderBy(stat => stat.ApiName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Nearest-rank percentile of a pre-sorted ascending list (null when empty).</summary>
    private static int? Percentile(IReadOnlyList<int> sorted, double p)
    {
        if (sorted.Count == 0)
        {
            return null;
        }
        var rank = (int)Math.Ceiling(p * sorted.Count);
        return sorted[Math.Clamp(rank - 1, 0, sorted.Count - 1)];
    }

    private static int? Average(IReadOnlyList<int> values) =>
        values.Count == 0 ? null : (int)Math.Round(values.Average());

    private static long? AverageLong(IReadOnlyList<int> values) =>
        values.Count == 0 ? null : (long)Math.Round(values.Average());

    /// <summary>Equal-width bins over the combined [0, max] duration range; empty when there is no data.</summary>
    private static ReportPerfHistogram BuildHistogram(IReadOnlyList<int> sortedA, IReadOnlyList<int> sortedB)
    {
        var maxA = sortedA.Count > 0 ? sortedA[^1] : 0;
        var maxB = sortedB.Count > 0 ? sortedB[^1] : 0;
        var max = Math.Max(maxA, maxB);
        if (max <= 0)
        {
            return new ReportPerfHistogram(0, []);
        }

        var binSize = NiceBinSize(max, TargetBins);
        var binCount = Math.Clamp((int)Math.Ceiling((double)(max + 1) / binSize), 1, 60);

        var countsA = new int[binCount];
        var countsB = new int[binCount];
        foreach (var value in sortedA)
        {
            countsA[Math.Min(value / binSize, binCount - 1)]++;
        }
        foreach (var value in sortedB)
        {
            countsB[Math.Min(value / binSize, binCount - 1)]++;
        }

        var bins = new List<ReportPerfBin>(binCount);
        for (var i = 0; i < binCount; i++)
        {
            bins.Add(new ReportPerfBin(i * binSize, (i + 1) * binSize, countsA[i], countsB[i]));
        }
        return new ReportPerfHistogram(binSize, bins);
    }

    /// <summary>Rounds the raw bin width (max/target) up to a friendly 1/2/5 × 10ⁿ value.</summary>
    private static int NiceBinSize(int max, int targetBins)
    {
        var raw = Math.Max(1.0, (double)max / targetBins);
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var normalized = raw / magnitude;
        var nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
        return Math.Max(1, (int)(nice * magnitude));
    }
}
