using HackOMania.Api.Entities;

namespace HackOMania.Api.Services;

public interface IGitHubRepositoryAutomationService
{
    Task ValidateAndMaybeForkAsync(
        HackathonGitHubRepositorySettings? settings,
        Uri repositoryUri,
        CancellationToken ct = default
    );
}
