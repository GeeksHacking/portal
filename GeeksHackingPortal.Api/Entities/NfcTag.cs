using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public class NfcTag
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid UserId {get;set;}

    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public User User { get; set; } = null!;
}
