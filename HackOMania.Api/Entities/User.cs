using SqlSugar;

namespace HackOMania.Api.Entities;

public class User
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(HackathonUser.UserId))]
    public List<HackathonUser> Hackathons { get; set; } = null!;
}
