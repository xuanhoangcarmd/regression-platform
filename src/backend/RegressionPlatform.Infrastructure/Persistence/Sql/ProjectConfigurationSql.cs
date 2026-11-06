namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>Parameterized SQL for project environment configuration and its headers.</summary>
internal static class ProjectConfigurationSql
{
    public const string GetProjectIdByCode = "SELECT Id FROM Project WHERE Code = @Code AND IsActive = 1;";

    /// <summary>Two result sets: the project's configurations, then all their headers.</summary>
    public const string GetByProject =
        "SELECT Id, ProjectId, Environment, BaseUrl FROM ProjectConfiguration WHERE ProjectId = @ProjectId;"
        + "SELECT h.Id, h.ConfigurationId, h.HeaderKey, h.HeaderValue, h.SecretValueEncrypted, h.IsSecret "
        + "FROM ProjectConfigurationHeader h "
        + "INNER JOIN ProjectConfiguration c ON c.Id = h.ConfigurationId "
        + "WHERE c.ProjectId = @ProjectId ORDER BY h.Id;";

    public const string GetConfigurationId =
        "SELECT Id FROM ProjectConfiguration WHERE ProjectId = @ProjectId AND Environment = @Environment;";

    public const string InsertConfiguration =
        "INSERT INTO ProjectConfiguration (ProjectId, Environment, BaseUrl) "
        + "OUTPUT INSERTED.Id VALUES (@ProjectId, @Environment, @BaseUrl);";

    public const string UpdateConfiguration =
        "UPDATE ProjectConfiguration SET BaseUrl = @BaseUrl, UpdatedDate = SYSUTCDATETIME() WHERE Id = @Id;";

    public const string DeleteHeaders = "DELETE FROM ProjectConfigurationHeader WHERE ConfigurationId = @ConfigurationId;";

    public const string InsertHeader =
        "INSERT INTO ProjectConfigurationHeader (ConfigurationId, HeaderKey, HeaderValue, SecretValueEncrypted, IsSecret) "
        + "VALUES (@ConfigurationId, @HeaderKey, @HeaderValue, @SecretValueEncrypted, @IsSecret);";
}
