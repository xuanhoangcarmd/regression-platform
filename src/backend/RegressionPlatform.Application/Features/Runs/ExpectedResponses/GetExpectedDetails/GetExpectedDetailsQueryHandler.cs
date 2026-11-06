using MediatR;
using RegressionPlatform.Application.Features.Projects;
using RegressionPlatform.Application.Features.Runs.Contracts;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses.GetExpectedDetails;

public sealed class GetExpectedDetailsQueryHandler(
    IExpectedResponseRepository repository,
    IRunRepository runRepository,
    IProjectRepository projectRepository
) : IRequestHandler<GetExpectedDetailsQuery, Result<IReadOnlyList<ExpectedResponseDetail>>>
{
    public async Task<Result<IReadOnlyList<ExpectedResponseDetail>>> Handle(
        GetExpectedDetailsQuery request,
        CancellationToken cancellationToken
    )
    {
        var (_, error) = await ExpectedResponseRunGuard.FindAsync(
            projectRepository, runRepository, request.ProjectCode, request.RunId, cancellationToken);
        if (error is not null)
        {
            return Result<IReadOnlyList<ExpectedResponseDetail>>.NotFound(error);
        }

        var ids = request.RequestIds.Distinct().ToList();
        return Result<IReadOnlyList<ExpectedResponseDetail>>.Success(
            await repository.GetDetailsAsync(request.RunId, ids, cancellationToken));
    }
}
