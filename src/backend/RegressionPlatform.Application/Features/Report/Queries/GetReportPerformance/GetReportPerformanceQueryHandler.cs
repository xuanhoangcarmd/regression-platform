using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetReportPerformance;

public sealed class GetReportPerformanceQueryHandler(
    IReportRepository repository,
    IProjectRepository projectRepository
) : IRequestHandler<GetReportPerformanceQuery, Result<ReportPerformanceResponse>>
{
    public async Task<Result<ReportPerformanceResponse>> Handle(
        GetReportPerformanceQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<ReportPerformanceResponse>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var report = await repository.GetByIdAsync(request.ReportId, cancellationToken);
        if (report is null || report.ProjectId != projectId.Value)
        {
            return Result<ReportPerformanceResponse>.NotFound(
                new Error("Report.NotFound", $"Report {request.ReportId} does not exist in the project.")
            );
        }

        var (perRequest, perApi) = await repository.GetPerformanceAsync(
            report.RunAId,
            report.RunBId,
            cancellationToken
        );

        // A run can have thousands of requests; that many points make the "Response Time by Request"
        // chart unreadable and the payload large. Downsample to ~200 evenly-spaced buckets so the
        // chart stays legible and the response small. Per-API grouping is preserved so the chart's
        // API filter still works.
        var perRequestForChart = Downsample(perRequest);

        var response = new ReportPerformanceResponse(
            report.RunAId,
            report.RunBId,
            report.RunATitle,
            report.RunBTitle,
            perRequestForChart,
            perApi
        );
        return Result<ReportPerformanceResponse>.Success(response);
    }

    /// <summary>Target number of points on the per-request chart (kept small for readability + payload).</summary>
    private const int MaxRequestPoints = 200;

    /// <summary>
    /// Reduces per-request timings to at most ~<see cref="MaxRequestPoints"/> points by averaging
    /// evenly-sized buckets. Buckets never cross API boundaries (each API gets a share of the budget
    /// proportional to its request count, at least one), so every emitted point still belongs to a
    /// single API and the client's API filter keeps working. Returns the input unchanged when small.
    /// </summary>
    private static IReadOnlyList<ReportRequestTiming> Downsample(IReadOnlyList<ReportRequestTiming> rows)
    {
        if (rows.Count <= MaxRequestPoints)
        {
            return rows;
        }

        // Group by API, preserving first-seen API order and each API's internal (request) order.
        var groups = new List<List<ReportRequestTiming>>();
        var groupIndex = new Dictionary<int, int>();
        foreach (var row in rows)
        {
            if (!groupIndex.TryGetValue(row.ApiId, out var gi))
            {
                gi = groups.Count;
                groupIndex[row.ApiId] = gi;
                groups.Add([]);
            }
            groups[gi].Add(row);
        }

        var total = rows.Count;
        var result = new List<ReportRequestTiming>(MaxRequestPoints + groups.Count);
        foreach (var items in groups)
        {
            var buckets = (int)Math.Round((double)items.Count * MaxRequestPoints / total);
            buckets = Math.Clamp(buckets, 1, items.Count);
            for (var b = 0; b < buckets; b++)
            {
                var start = (int)((long)b * items.Count / buckets);
                var end = (int)((long)(b + 1) * items.Count / buckets); // exclusive
                if (end <= start)
                {
                    end = start + 1;
                }

                // Representative row (identity/label from the bucket's first request) with averaged durations.
                result.Add(items[start] with
                {
                    DurationMsA = AverageDuration(items, start, end, static t => t.DurationMsA),
                    DurationMsB = AverageDuration(items, start, end, static t => t.DurationMsB),
                });
            }
        }
        return result;
    }

    /// <summary>Rounded average of a duration selector over items[start, end), ignoring nulls (null if all null).</summary>
    private static int? AverageDuration(
        List<ReportRequestTiming> items,
        int start,
        int end,
        Func<ReportRequestTiming, int?> select
    )
    {
        long sum = 0;
        var count = 0;
        for (var i = start; i < end; i++)
        {
            var value = select(items[i]);
            if (value.HasValue)
            {
                sum += value.Value;
                count++;
            }
        }
        return count == 0 ? null : (int)Math.Round((double)sum / count);
    }
}
