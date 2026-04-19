using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public class HackathonGitHubRepositorySettings
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid HackathonId { get; set; }

    public bool IsRepositoryCheckingEnabled { get; set; }

    public bool IsRepositoryForkingEnabled { get; set; }

    [SugarColumn(ColumnDataType = "nvarchar(512)", IsNullable = true)]
    public string? ApiKey { get; set; }

    [SugarColumn(ColumnDataType = "nvarchar(128)", IsNullable = true)]
    public string? RepositoryPrefix { get; set; }

    public long? OrganizationId { get; set; }
}
