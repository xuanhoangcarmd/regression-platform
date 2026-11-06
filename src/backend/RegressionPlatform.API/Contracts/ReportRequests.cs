namespace RegressionPlatform.API.Contracts;

/// <summary>Request body for generating (or regenerating) a report from a Completed compare.</summary>
public sealed record GenerateReportRequest(long CompareId);

/// <summary>Request body for the "Ask AI about this report" box.</summary>
public sealed record AskReportAiQuestionRequest(string Question);
