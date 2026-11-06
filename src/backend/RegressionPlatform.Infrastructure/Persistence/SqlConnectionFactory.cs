using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RegressionPlatform.Application.Abstractions;

namespace RegressionPlatform.Infrastructure.Persistence;

/// <summary>SQL Server implementation of <see cref="IDbConnectionFactory"/> (Dapper).</summary>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("RegressionDB")
            ?? throw new InvalidOperationException("Connection string 'RegressionDB' is not configured.");
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
