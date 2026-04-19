using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public abstract class OnlineAccount
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(UserId))]
    public User User { get; set; } = null!;
}
