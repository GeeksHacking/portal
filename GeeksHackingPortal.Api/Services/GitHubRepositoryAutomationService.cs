using GeeksHackingPortal.Api.Entities;
using Octokit;
using Polly;
using Polly.Retry;
using System.Runtime.ExceptionServices;
using System.Text;

namespace GeeksHackingPortal.Api.Services;

public class GitHubRepositoryAutomationService(
    ILogger<GitHubRepositoryAutomationService> logger
) : IGitHubRepositoryAutomationService
{
    public async Task ValidateAndMaybeForkAsync(
        HackathonGitHubRepositorySettings? settings,
        string teamName,
        Uri repositoryUri,
        CancellationToken ct = default
    )
    {
        if (
            settings is null
            || (!settings.IsRepositoryCheckingEnabled && !settings.IsRepositoryForkingEnabled)
        )
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new GitHubRepositoryAutomationException(
                "GitHub repository automation is enabled, but no API key is configured."
            );
        }

        var coordinates = ParseRepositoryCoordinates(repositoryUri);
        var client = CreateClient(settings.ApiKey.Trim());

        Repository repository;
        try
        {
            repository = await client.Repository.Get(coordinates.Owner, coordinates.Name);
        }
        catch (NotFoundException ex)
        {
            throw new GitHubRepositoryAutomationException(
                "The submitted repository could not be found on GitHub.",
                ex
            );
        }
        catch (AuthorizationException ex)
        {
            throw new GitHubRepositoryAutomationException(
                "GitHub rejected the configured API key while validating the repository.",
                ex
            );
        }
        catch (ApiException ex)
        {
            logger.LogWarning(
                ex,
                "GitHub repository validation failed for {Owner}/{Repository}",
                coordinates.Owner,
                coordinates.Name
            );
            throw new GitHubRepositoryAutomationException(
                "GitHub validation failed for the submitted repository.",
                ex
            );
        }

        if (!settings.IsRepositoryForkingEnabled)
        {
            return;
        }

        if (!settings.OrganizationId.HasValue)
        {
            throw new GitHubRepositoryAutomationException(
                "Repository cloning is enabled, but no target GitHub organization is configured."
            );
        }

        Organization organization;
        try
        {
            organization = await client.Organization.Get(settings.OrganizationId.Value.ToString());
        }
        catch (NotFoundException ex)
        {
            throw new GitHubRepositoryAutomationException(
                "The configured GitHub organization could not be found.",
                ex
            );
        }
        catch (AuthorizationException ex)
        {
            throw new GitHubRepositoryAutomationException(
                "GitHub rejected the configured API key while resolving the target organization.",
                ex
            );
        }
        catch (ApiException ex)
        {
            logger.LogWarning(
                ex,
                "GitHub organization lookup failed for organization id {OrganizationId}",
                settings.OrganizationId.Value
            );
            throw new GitHubRepositoryAutomationException(
                "GitHub validation failed for the configured organization.",
                ex
            );
        }

        Repository fork;
        try
        {
            fork = await client.Repository.Forks.Create(
                repository.Owner.Login,
                repository.Name,
                new NewRepositoryFork { Organization = organization.Login }
            );
        }
        catch (AuthorizationException ex)
        {
            throw new GitHubRepositoryAutomationException(
                "GitHub rejected the configured API key while forking the repository.",
                ex
            );
        }
        catch (ApiValidationException ex)
        {
            throw new GitHubRepositoryAutomationException(
                "GitHub could not fork the submitted repository with the configured settings.",
                ex
            );
        }
        catch (ApiException ex)
        {
            logger.LogWarning(
                ex,
                "GitHub fork creation failed for {Owner}/{Repository}",
                repository.Owner.Login,
                repository.Name
            );
            throw new GitHubRepositoryAutomationException(
                "GitHub failed to fork the submitted repository.",
                ex
            );
        }

        logger.LogInformation(
            "GitHub fork created from {SourceRepositoryUrl} to {ForkRepositoryUrl}",
            repository.HtmlUrl,
            fork.HtmlUrl
        );

        var targetName = BuildTargetRepositoryName(
            settings.RepositoryPrefix,
            teamName,
            repository.Name
        );
        if (string.Equals(fork.Name, targetName, StringComparison.Ordinal))
        {
            logger.LogInformation(
                "GitHub fork already uses the target name. Source: {SourceRepositoryUrl}. Final fork: {FinalForkRepositoryUrl}",
                repository.HtmlUrl,
                fork.HtmlUrl
            );
            return;
        }

        var renamedFork = fork;
        try
        {
            renamedFork = await RenameForkAsync(client, fork.Id, targetName, ct);
        }
        catch (AuthorizationException ex)
        {
            throw new GitHubRepositoryAutomationException(
                "GitHub rejected the configured API key while renaming the forked repository.",
                ex
            );
        }
        catch (ApiValidationException ex)
        {
            throw new GitHubRepositoryAutomationException(
                "GitHub could not rename the forked repository with the configured name.",
                ex
            );
        }
        catch (ApiException ex)
        {
            logger.LogWarning(
                ex,
                "GitHub fork rename failed for fork id {ForkId} to {TargetName}",
                fork.Id,
                targetName
            );
            throw new GitHubRepositoryAutomationException(
                "GitHub failed to rename the forked repository with the configured name.",
                ex
            );
        }

        logger.LogInformation(
            "GitHub fork renamed. Source: {SourceRepositoryUrl}. Original fork: {OriginalForkRepositoryUrl}. Final fork: {FinalForkRepositoryUrl}",
            repository.HtmlUrl,
            fork.HtmlUrl,
            renamedFork.HtmlUrl
        );
    }

    private static GitHubClient CreateClient(string apiKey)
    {
        var client = new GitHubClient(new ProductHeaderValue(nameof(GitHubRepositoryAutomationService)));
        client.Credentials = new Credentials(apiKey);
        return client;
    }

    private static string BuildTargetRepositoryName(
        string? prefix,
        string teamName,
        string repositoryName
    )
    {
        var parts = new[] { prefix, teamName, repositoryName }
            .Select(SanitizeRepositoryNamePart)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        if (parts.Length == 0)
        {
            throw new GitHubRepositoryAutomationException(
                "GitHub could not derive a valid name for the forked repository."
            );
        }

        return string.Join("-", parts);
    }

    private async Task<Repository> RenameForkAsync(
        GitHubClient client,
        long forkId,
        string targetName,
        CancellationToken ct
    )
    {
        var retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = 4,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = false,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<NotFoundException>()
                        .Handle<RetryableForkRenameException>(),
                    OnRetry = args =>
                    {
                        logger.LogInformation(
                            args.Outcome.Exception,
                            "GitHub fork rename is not ready yet for fork id {ForkId}; retrying rename to {TargetName}",
                            forkId,
                            targetName
                        );
                        return default;
                    },
                }
            )
            .Build();

        try
        {
            return await retryPipeline.ExecuteAsync(
                async token => await TryRenameForkAsync(client, forkId, targetName, token),
                ct
            );
        }
        catch (RetryableForkRenameException ex) when (ex.InnerException is ApiValidationException apiEx)
        {
            ExceptionDispatchInfo.Capture(apiEx).Throw();
            throw;
        }
    }

    private static string? SanitizeRepositoryNamePart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_' or '.')
            {
                builder.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (previousWasSeparator)
            {
                continue;
            }

            builder.Append('-');
            previousWasSeparator = true;
        }

        return builder.ToString().Trim('-', '.', '_');
    }

    private static async Task<Repository?> TryGetRepositoryAsync(GitHubClient client, long repositoryId)
    {
        try
        {
            return await client.Repository.Get(repositoryId);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private static async ValueTask<Repository> TryRenameForkAsync(
        GitHubClient client,
        long forkId,
        string targetName,
        CancellationToken ct
    )
    {
        try
        {
            return await client.Repository.Edit(
                forkId,
                new RepositoryUpdate { Name = targetName }
            );
        }
        catch (ApiValidationException ex)
        {
            var existingRepository = await TryGetRepositoryAsync(client, forkId);
            if (
                existingRepository is not null
                && string.Equals(existingRepository.Name, targetName, StringComparison.Ordinal)
            )
            {
                return existingRepository;
            }

            ct.ThrowIfCancellationRequested();
            throw new RetryableForkRenameException(ex);
        }
    }

    private static (string Owner, string Name) ParseRepositoryCoordinates(Uri repositoryUri)
    {
        if (
            !string.Equals(repositoryUri.Host, "github.com", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(repositoryUri.Host, "www.github.com", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new GitHubRepositoryAutomationException(
                "The submitted repository must be a GitHub repository URL."
            );
        }

        var segments = repositoryUri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2)
        {
            throw new GitHubRepositoryAutomationException(
                "The submitted repository URL must include both the owner and repository name."
            );
        }

        var owner = segments[0];
        var repositoryName = segments[1];
        if (repositoryName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            repositoryName = repositoryName[..^4];
        }

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repositoryName))
        {
            throw new GitHubRepositoryAutomationException(
                "The submitted repository URL must include both the owner and repository name."
            );
        }

        return (owner, repositoryName);
    }

    private sealed class RetryableForkRenameException(ApiValidationException innerException)
        : Exception("The forked repository is not ready to be renamed yet.", innerException);
}
