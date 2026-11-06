using MediatR;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.Application.Features.ApiDefinition.Commands.DeleteApi;

/// <summary>Deletes an API definition by id.</summary>
public sealed record DeleteApiCommand(int Id) : IRequest<Result>;
