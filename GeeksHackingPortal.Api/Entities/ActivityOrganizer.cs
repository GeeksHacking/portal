using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

[SugarIndex(
    "IX_ActivityOrganizer_ActivityId_UserId",
    nameof(ActivityId),
    OrderByType.Asc,
    nameof(UserId),
    OrderByType.Asc,
    IsUnique = true
)]
public class ActivityOrganizer
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid ActivityId { get; set; }

    public Guid UserId { get; set; }

    public OrganizerType Type { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Navigate(NavigateType.ManyToOne, nameof(ActivityId))]
    public Activity Activity { get; set; } = null!;

    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public User User { get; set; } = null!;
}
