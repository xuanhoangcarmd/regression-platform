namespace RegressionPlatform.Shared.Results;

/// <summary>Outcome states for a business operation (see docs/rules/dotnet/result-pattern.md).</summary>
public enum ResultStatus
{
    Success,
    Failure,
    ValidationFailure,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
}

/// <summary>A single error (code + human-readable message).</summary>
public sealed record Error(string Code, string Message);

/// <summary>
/// Explicit success/failure outcome for business operations — used instead of throwing.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, ResultStatus status, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Status = status;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public ResultStatus Status { get; }
    public IReadOnlyList<Error> Errors { get; }

    public static Result Success() => new(true, ResultStatus.Success, []);
    public static Result Failure(params Error[] errors) => new(false, ResultStatus.Failure, errors);
    public static Result NotFound(params Error[] errors) => new(false, ResultStatus.NotFound, errors);
    public static Result Conflict(params Error[] errors) => new(false, ResultStatus.Conflict, errors);
    public static Result ValidationFailure(IReadOnlyList<Error> errors) => new(false, ResultStatus.ValidationFailure, errors);
    public static Result Unauthorized(params Error[] errors) => new(false, ResultStatus.Unauthorized, errors);
    public static Result Forbidden(params Error[] errors) => new(false, ResultStatus.Forbidden, errors);
}

/// <summary>A <see cref="Result"/> that carries a value on success.</summary>
public sealed class Result<T> : Result
{
    private Result(T value)
        : base(true, ResultStatus.Success, [])
    {
        Value = value;
    }

    private Result(ResultStatus status, IReadOnlyList<Error> errors)
        : base(false, status, errors) { }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(params Error[] errors) => new(ResultStatus.Failure, errors);
    public static new Result<T> NotFound(params Error[] errors) => new(ResultStatus.NotFound, errors);
    public static new Result<T> Conflict(params Error[] errors) => new(ResultStatus.Conflict, errors);
    public static new Result<T> ValidationFailure(IReadOnlyList<Error> errors) => new(ResultStatus.ValidationFailure, errors);
    public static new Result<T> Unauthorized(params Error[] errors) => new(ResultStatus.Unauthorized, errors);
    public static new Result<T> Forbidden(params Error[] errors) => new(ResultStatus.Forbidden, errors);

    /// <summary>Lets handlers `return value;` for the success path.</summary>
    public static implicit operator Result<T>(T value) => Success(value);
}
