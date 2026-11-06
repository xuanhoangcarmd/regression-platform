namespace RegressionPlatform.Application.Features.Runs.ExpectedResponses;

/// <summary>Limits for the Expected Response endpoints.</summary>
public static class ExpectedResponseLimits
{
    /// <summary>Max requests per details / save / restore call (the UI batches larger selections).</summary>
    public const int MaxBatch = 1000;

    public const int ReasonMaxLength = 500;
}
