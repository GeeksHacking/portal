using GeeksHackingPortal.Api.Entities;

namespace GeeksHackingPortal.Api.Services;

public interface IGitHubRepositoryAutomationService
{
    Task ValidateAndMaybeForkAsync(
        HackathonGitHubRepositorySettings? settings,
        string teamName,
        Uri repositoryUri,
        CancellationToken ct = default
    );
}
