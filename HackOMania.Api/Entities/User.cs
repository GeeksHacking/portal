using SqlSugar;

namespace HackOMania.Api.Entities;

public class User
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    [SugarColumn(IsIgnore = true)]
    public string Name => $"{FirstName} {LastName}".Trim();

    public string Email { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(HackathonUser.UserId))]
    public List<HackathonUser> Hackathons { get; set; } = null!;
}
