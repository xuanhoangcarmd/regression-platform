using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.Queries.GetRunResponseDetail;

public sealed class GetRunResponseDetailQueryHandler(IRunRepository repository, IProjectRepository projectRepository)
    : IRequestHandler<GetRunResponseDetailQuery, Result<RunResponseDetail>>
{
    public async Task<Result<RunResponseDetail>> Handle(
        GetRunResponseDetailQuery request,
        CancellationToken cancellationToken
    )
    {
        var projectId = await projectRepository.GetIdByCodeAsync(request.ProjectCode, cancellationToken);
        if (projectId is null)
        {
            return Result<RunResponseDetail>.NotFound(
                new Error("Project.NotFound", $"Project '{request.ProjectCode}' does not exist.")
            );
        }

        var run = await repository.GetEntityAsync(request.RunId, cancellationToken);
        if (run is null || run.ProjectId != projectId.Value)
        {
            return Result<RunResponseDetail>.NotFound(
                new Error("Run.NotFound", $"Run {request.RunId} does not exist in the project.")
            );
        }

        var detail = await repository.GetResponseDetailAsync(request.RunId, request.RequestId, cancellationToken);
        if (detail is null)
        {
            return Result<RunResponseDetail>.NotFound(
                new Error("RunResponse.NotFound", $"Request {request.RequestId} has no response in run {request.RunId}.")
            );
        }

        return Result<RunResponseDetail>.Success(detail);
    }
}
