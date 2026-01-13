using SqlSugar;

namespace HackOMania.Api.Entities;

[SugarIndex("UK_GitHubOnlineAccount_GitHubId", nameof(GitHubId), OrderByType.Asc, IsUnique = true)]
[SugarIndex(
    "UK_GitHubOnlineAccount_GitHubLogin",
    nameof(GitHubLogin),
    OrderByType.Asc,
    IsUnique = true
)]
public class GitHubOnlineAccount : OnlineAccount
{
    public long GitHubId { get; set; }
    public string GitHubLogin { get; set; } = null!;
}
