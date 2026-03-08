using HackOMania.Api.Entities;

namespace HackOMania.Api.Features.Hackathons.GitHubRepositorySettings;

public static class HackathonGitHubRepositorySettingsMapper
{
    public static HackathonGitHubRepositorySettingsResponse ToResponse(
        HackathonGitHubRepositorySettings? settings
    )
    {
        return new HackathonGitHubRepositorySettingsResponse
        {
            IsRepositoryCheckingEnabled = settings?.IsRepositoryCheckingEnabled ?? false,
            IsRepositoryForkingEnabled = settings?.IsRepositoryForkingEnabled ?? false,
            HasApiKey = !string.IsNullOrWhiteSpace(settings?.ApiKey),
            RepositoryPrefix = settings?.RepositoryPrefix,
            OrganizationId = settings?.OrganizationId,
        };
    }
}
