using SqlSugar;

namespace HackOMania.Api.Entities;

public class Workshop
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid HackathonId { get; set; }

    public string Title { get; set; } = null!;

    [SugarColumn(ColumnDataType = "longtext")]
    public string Description { get; set; } = null!;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public string Location { get; set; } = null!;

    public int MaxParticipants { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Navigate(NavigateType.OneToMany, nameof(WorkshopParticipant.WorkshopId))]
    public List<WorkshopParticipant> Participants { get; set; } = null!;
}
