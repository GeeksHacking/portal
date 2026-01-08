using HackOMania.Api.Converters;
using SqlSugar;

namespace HackOMania.Api.Entities;

public class ChallengeSubmission
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }

    [SugarColumn(ColumnDataType = "nvarchar(64)", SqlParameterDbType = typeof(UriConverter))]
    public Uri RepositoryUri { get; set; }

    [SugarColumn(ColumnDataType = "nvarchar(64)", SqlParameterDbType = typeof(UriConverter))]
    public Uri DemoUri { get; set; }

    [SugarColumn(ColumnDataType = "nvarchar(64)", SqlParameterDbType = typeof(UriConverter))]
    public Uri SlidesUri { get; set; }

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid HackathonId { get; set; }

    public Guid TeamId { get; set; }
    public Guid ChallengeId { get; set; }

    public Guid SubmittedByUserId { get; set; }
}
