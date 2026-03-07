using SqlSugar;

namespace HackOMania.Api.Entities;

public class Resource
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    /// <summary>
    /// Statement condition passed to Jint to evaluate whether redemption is allowed
    /// </summary>
    public string RedemptionStmt { get; set; } = "true";

    public bool IsPublished { get; set; }

    public Guid HackathonId { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(ResourceRedemption.ResourceId))]
    public List<ResourceRedemption>? Redemptions { get; set; } = null!;
}
