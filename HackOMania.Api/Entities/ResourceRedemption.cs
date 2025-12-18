using SqlSugar;

namespace HackOMania.Api.Entities;

public class ResourceRedemption
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid ResourceId { get; set; }

    public Guid HackathonId { get; set; }

    public Guid RedeemerId { get; set; }
}
