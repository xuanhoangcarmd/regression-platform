namespace RegressionPlatform.Application.Features.ApiDefinition.Contracts;

/// <summary>Allowed HTTP methods for an API definition (matches the <c>Method varchar(10)</c> column).</summary>
public static class ApiHttpMethods
{
    public static readonly IReadOnlyList<string> Allowed = ["GET", "POST", "PUT", "PATCH", "DELETE"];
}
