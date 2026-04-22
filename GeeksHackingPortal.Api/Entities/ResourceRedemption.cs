using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public class ResourceRedemption
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid ResourceId { get; set; }

    [SugarColumn(OldColumnName = "HackathonId")]
    public Guid ActivityId { get; set; }

    [SugarColumn(OldColumnName = "RedeemerId")]
    public Guid UserId { get; set; }

    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public User User { get; set; } = null!;
}
