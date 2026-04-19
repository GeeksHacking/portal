using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public class EventTimelineItem
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid HackathonId { get; set; }

    public string Title { get; set; } = null!;

    [SugarColumn(IsNullable = true, ColumnDataType = "longtext")]
    public string? Description { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    [Navigate(NavigateType.ManyToOne, nameof(HackathonId))]
    public Hackathon Hackathon { get; set; } = null!;
}
