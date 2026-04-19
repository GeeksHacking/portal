namespace GeeksHackingPortal.Api.Features.Hackathons.GitHubRepositorySettings;

public class HackathonGitHubRepositorySettingsRequest
{
    public bool IsRepositoryCheckingEnabled { get; set; }
    public bool IsRepositoryForkingEnabled { get; set; }
    public string? ApiKey { get; set; }
    public bool ClearApiKey { get; set; }
    public string? RepositoryPrefix { get; set; }
    public long? OrganizationId { get; set; }
}
