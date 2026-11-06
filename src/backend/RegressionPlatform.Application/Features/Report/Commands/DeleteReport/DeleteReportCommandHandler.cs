using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Report.Commands.DeleteReport;

public sealed class DeleteReportCommandHandler(IReportRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<DeleteReportCommand, Result>
{
    public async Task<Result> Handle(DeleteReportCommand request, CancellationToken cancellationToken)
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result.NotFound(new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist."));
        }

        var report = await repository.GetByIdAsync(request.ReportId, cancellationToken);
        if (report is null || report.ProjectId != projectId.Value)
        {
            return Result.NotFound(
                new Error("Report.NotFound", $"Report {request.ReportId} does not exist in the project.")
            );
        }

        await repository.DeleteAsync(request.ReportId, cancellationToken);
        return Result.Success();
    }
}
