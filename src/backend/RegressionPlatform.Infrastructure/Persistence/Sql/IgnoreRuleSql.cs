namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>Parameterized SQL for compare ignore rules and their API links.</summary>
internal static class IgnoreRuleSql
{
    private const string Columns = "Id, JsonPath, Description, Enabled, RuleType, Delimiter";

    private const string LinksSelect =
        "SELECT ra.IgnoreRuleId, a.Id AS ApiId, a.Name AS ApiName "
        + "FROM CompareIgnoreRuleApi ra "
        + "INNER JOIN Api a ON a.Id = ra.ApiId "
        + "INNER JOIN CompareIgnoreRule c ON c.Id = ra.IgnoreRuleId";

    /// <summary>Two result sets: the project's rules, then their (rule → API) links.</summary>
    public const string GetAll =
        $"SELECT {Columns} FROM CompareIgnoreRule WHERE ProjectId = @ProjectId ORDER BY Id;"
        + LinksSelect
        + " WHERE c.ProjectId = @ProjectId ORDER BY a.Name;";

    /// <summary>Two result sets: the rule, then its (rule → API) links.</summary>
    public const string GetById =
        $"SELECT {Columns} FROM CompareIgnoreRule WHERE Id = @Id;"
        + LinksSelect
        + " WHERE ra.IgnoreRuleId = @Id ORDER BY a.Name;";

    public const string Insert =
        "INSERT INTO CompareIgnoreRule (ProjectId, JsonPath, Description, Enabled, RuleType, Delimiter) "
        + "OUTPUT INSERTED.Id VALUES (@ProjectId, @JsonPath, @Description, @Enabled, @RuleType, @Delimiter);";

    public const string Update =
        "UPDATE CompareIgnoreRule SET JsonPath = @JsonPath, Description = @Description, Enabled = @Enabled, RuleType = @RuleType, "
        + "Delimiter = @Delimiter WHERE Id = @Id;";

    public const string Delete = "DELETE FROM CompareIgnoreRule WHERE Id = @Id;";

    public const string DeleteLinks = "DELETE FROM CompareIgnoreRuleApi WHERE IgnoreRuleId = @IgnoreRuleId;";

    public const string InsertLink =
        "INSERT INTO CompareIgnoreRuleApi (IgnoreRuleId, ApiId) VALUES (@IgnoreRuleId, @ApiId);";
}
