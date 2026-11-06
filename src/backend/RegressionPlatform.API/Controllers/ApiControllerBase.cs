using Microsoft.AspNetCore.Mvc;
using RegressionPlatform.Shared.Results;

namespace RegressionPlatform.API.Controllers;

/// <summary>
/// Base controller that maps the Result pattern to HTTP responses + ProblemDetails,
/// so controllers stay thin and status-code mapping lives in one place.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Success → 204 No Content; otherwise the mapped problem response.</summary>
    protected IActionResult HandleResult(Result result) => result.IsSuccess ? NoContent() : MapProblem(result);

    /// <summary>Success → 200 with value (or 204 if null); otherwise the mapped problem response.</summary>
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (!result.IsSuccess)
        {
            return MapProblem(result);
        }

        return result.Value is null ? NoContent() : Ok(result.Value);
    }

    /// <summary>Success → 201 Created (at <paramref name="actionName"/>); otherwise the mapped problem response.</summary>
    protected IActionResult HandleCreated<T>(Result<T> result, string actionName, object routeValues) =>
        result.IsSuccess ? CreatedAtAction(actionName, routeValues, result.Value) : MapProblem(result);

    private IActionResult MapProblem(Result result)
    {
        var statusCode = result.Status switch
        {
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            ResultStatus.ValidationFailure => StatusCodes.Status400BadRequest,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };

        if (result.Status == ResultStatus.ValidationFailure)
        {
            var errors = result
                .Errors.GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray());

            return ValidationProblem(new ValidationProblemDetails(errors) { Status = statusCode });
        }

        var detail = result.Errors.Count > 0 ? string.Join("; ", result.Errors.Select(error => error.Message)) : null;
        return Problem(statusCode: statusCode, detail: detail);
    }
}
