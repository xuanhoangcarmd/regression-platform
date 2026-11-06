using System.Data;

namespace RegressionPlatform.Application.Abstractions;

/// <summary>
/// Creates short-lived database connections for repositories (see dapper.md).
/// Implemented in Infrastructure; consumed only via this Application interface.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Returns a new, unopened connection. Dapper opens/closes as needed; callers dispose it.</summary>
    IDbConnection CreateConnection();
}
