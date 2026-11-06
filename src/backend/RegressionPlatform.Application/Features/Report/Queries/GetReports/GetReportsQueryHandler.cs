using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Report.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Queries.GetReports;

public sealed class GetReportsQueryHandler(IReportRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetReportsQuery, Result<IReadOnlyList<ReportListItem>>>
{
    public async Task<Result<IReadOnlyList<ReportListItem>>> Handle(
        GetReportsQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<IReadOnlyList<ReportListItem>>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var reports = await repository.GetByProjectAsync(projectId.Value, cancellationToken);
        return Result<IReadOnlyList<ReportListItem>>.Success(reports);
    }
}
