using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public abstract class HackathonUser
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid HackathonId { get; set; }

    public Guid UserId { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(ResourceRedemption.UserId), nameof(UserId))]
    public List<ResourceRedemption> Redemptions { get; set; } = null!;
}
