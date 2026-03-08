namespace HackOMania.Api.Features.Hackathons.GitHubRepositorySettings;

public class HackathonGitHubRepositorySettingsResponse
{
    public bool IsRepositoryCheckingEnabled { get; init; }
    public bool IsRepositoryForkingEnabled { get; init; }
    public bool HasApiKey { get; init; }
    public string? RepositoryPrefix { get; init; }
    public long? OrganizationId { get; init; }
}
