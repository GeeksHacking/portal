namespace HackOMania.Api.Entities;

public class GitHubOnlineAccount : OnlineAccount
{
    public long GitHubId { get; set; }
    public string GitHubLogin { get; set; } = null!;
}
