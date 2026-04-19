using GeeksHackingPortal.Api.Entities;

namespace GeeksHackingPortal.Api.Features.Hackathons.GitHubRepositorySettings;

public static class HackathonGitHubRepositorySettingsMutation
{
    public static HackathonGitHubRepositorySettingsMutationResult BuildForCreate(
        HackathonGitHubRepositorySettingsRequest? request
    )
    {
        if (!HasAnyValues(request))
        {
            return new(null, []);
        }

        var settings = new HackathonGitHubRepositorySettings
        {
            IsRepositoryCheckingEnabled = request!.IsRepositoryCheckingEnabled,
            IsRepositoryForkingEnabled = request.IsRepositoryForkingEnabled,
            ApiKey = string.IsNullOrWhiteSpace(request.ApiKey) ? null : request.ApiKey.Trim(),
            RepositoryPrefix = string.IsNullOrWhiteSpace(request.RepositoryPrefix)
                ? null
                : request.RepositoryPrefix.Trim(),
            OrganizationId = request.OrganizationId,
        };

        return new(settings, Validate(settings));
    }

    public static HackathonGitHubRepositorySettingsMutationResult Apply(
        HackathonGitHubRepositorySettings? currentSettings,
        HackathonGitHubRepositorySettingsRequest request
    )
    {
        currentSettings ??= new HackathonGitHubRepositorySettings();

        currentSettings.IsRepositoryCheckingEnabled = request.IsRepositoryCheckingEnabled;
        currentSettings.IsRepositoryForkingEnabled = request.IsRepositoryForkingEnabled;
        currentSettings.RepositoryPrefix = string.IsNullOrWhiteSpace(request.RepositoryPrefix)
            ? null
            : request.RepositoryPrefix.Trim();
        currentSettings.OrganizationId = request.OrganizationId;

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            currentSettings.ApiKey = request.ApiKey.Trim();
        }
        else if (request.ClearApiKey)
        {
            currentSettings.ApiKey = null;
        }

        return new(currentSettings, Validate(currentSettings));
    }

    public static bool ShouldPersist(HackathonGitHubRepositorySettings? settings)
    {
        return settings is not null
            && (
                settings.IsRepositoryCheckingEnabled
                || settings.IsRepositoryForkingEnabled
                || settings.OrganizationId.HasValue
                || !string.IsNullOrWhiteSpace(settings.ApiKey)
                || !string.IsNullOrWhiteSpace(settings.RepositoryPrefix)
            );
    }

    private static bool HasAnyValues(HackathonGitHubRepositorySettingsRequest? settings)
    {
        if (settings is null)
        {
            return false;
        }

        return settings.IsRepositoryCheckingEnabled
            || settings.IsRepositoryForkingEnabled
            || settings.ClearApiKey
            || settings.OrganizationId.HasValue
            || !string.IsNullOrWhiteSpace(settings.ApiKey)
            || !string.IsNullOrWhiteSpace(settings.RepositoryPrefix);
    }

    private static List<HackathonGitHubRepositorySettingsValidationError> Validate(
        HackathonGitHubRepositorySettings settings
    )
    {
        var errors = new List<HackathonGitHubRepositorySettingsValidationError>();

        var requiresApiKey =
            settings.IsRepositoryCheckingEnabled || settings.IsRepositoryForkingEnabled;
        if (requiresApiKey && string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            errors.Add(
                new(
                    HackathonGitHubRepositorySettingsField.ApiKey,
                    "A GitHub API key is required when repository checking or cloning is enabled."
                )
            );
        }

        if (
            settings.IsRepositoryForkingEnabled
            && (!settings.OrganizationId.HasValue || settings.OrganizationId.Value <= 0)
        )
        {
            errors.Add(
                new(
                    HackathonGitHubRepositorySettingsField.OrganizationId,
                    "A valid GitHub organization id is required when repository cloning is enabled."
                )
            );
        }

        return errors;
    }
}

public sealed record HackathonGitHubRepositorySettingsMutationResult(
    HackathonGitHubRepositorySettings? Settings,
    IReadOnlyList<HackathonGitHubRepositorySettingsValidationError> Errors
)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed record HackathonGitHubRepositorySettingsValidationError(
    HackathonGitHubRepositorySettingsField Field,
    string Message
);

public enum HackathonGitHubRepositorySettingsField
{
    ApiKey,
    OrganizationId,
}
