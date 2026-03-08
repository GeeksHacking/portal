namespace HackOMania.Tests.Models;

public class GitHubRepositorySettingsRequest
{
    public bool IsRepositoryCheckingEnabled { get; set; }
    public bool IsRepositoryForkingEnabled { get; set; }
    public string? ApiKey { get; set; }
    public bool ClearApiKey { get; set; }
    public string? RepositoryPrefix { get; set; }
    public long? OrganizationId { get; set; }
}

public class GitHubRepositorySettingsResponse
{
    public bool IsRepositoryCheckingEnabled { get; set; }
    public bool IsRepositoryForkingEnabled { get; set; }
    public bool HasApiKey { get; set; }
    public string? RepositoryPrefix { get; set; }
    public long? OrganizationId { get; set; }
}
