using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Features.Hackathons.GitHubRepositorySettings;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Hackathons;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/hackathons/{HackathonId:guid}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Hackathons"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Includes(h => h.Activity)
            .InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var gitHubRepositorySettings = await sql.Queryable<HackathonGitHubRepositorySettings>()
            .Where(s => s.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            hackathon.Activity.Title = req.Name;
        }

        if (!string.IsNullOrWhiteSpace(req.Description))
        {
            hackathon.Activity.Description = req.Description;
        }

        if (!string.IsNullOrWhiteSpace(req.Venue))
        {
            hackathon.Activity.Location = req.Venue;
        }

        if (req.HomepageUri is not null)
        {
            hackathon.HomepageUri = req.HomepageUri;
        }

        if (!string.IsNullOrWhiteSpace(req.ShortCode) && req.ShortCode != hackathon.ShortCode)
        {
            var exists = await sql.Queryable<Entities.Hackathon>()
                .AnyAsync(h => h.Id != hackathon.Id && h.ShortCode == req.ShortCode, ct);
            if (exists)
            {
                AddError(r => r.ShortCode, "A hackathon with this short code already exists.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            hackathon.ShortCode = req.ShortCode;
        }

        if (req.IsPublished.HasValue)
        {
            hackathon.Activity.IsPublished = req.IsPublished.Value;
        }

        if (req.EventStartDate.HasValue)
        {
            hackathon.Activity.StartTime = req.EventStartDate.Value;
        }

        if (req.EventEndDate.HasValue)
        {
            hackathon.Activity.EndTime = req.EventEndDate.Value;
        }

        if (req.SubmissionsStartDate.HasValue)
        {
            hackathon.SubmissionsStartDate = req.SubmissionsStartDate.Value;
        }

        if (req.ChallengeSelectionEndDate.HasValue)
        {
            hackathon.ChallengeSelectionEndDate = req.ChallengeSelectionEndDate.Value;
        }

        if (req.SubmissionsEndDate.HasValue)
        {
            hackathon.SubmissionsEndDate = req.SubmissionsEndDate.Value;
        }

        if (req.JudgingStartDate.HasValue)
        {
            hackathon.JudgingStartDate = req.JudgingStartDate.Value;
        }

        if (req.JudgingEndDate.HasValue)
        {
            hackathon.JudgingEndDate = req.JudgingEndDate.Value;
        }

        if (req.GitHubRepositorySettings is not null)
        {
            var gitHubRepositorySettingsResult = HackathonGitHubRepositorySettingsMutation.Apply(
                gitHubRepositorySettings,
                req.GitHubRepositorySettings
            );
            AddGitHubRepositorySettingsErrors(gitHubRepositorySettingsResult.Errors);
            if (ValidationFailed)
            {
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            gitHubRepositorySettings = gitHubRepositorySettingsResult.Settings;
        }

        var normalizedTemplates = req.EmailTemplates is null
            ? null
            : EmailTemplateNormalizer.Normalize(req.EmailTemplates);

        await sql.Updateable(hackathon.Activity).ExecuteCommandAsync(ct);
        await sql.Updateable(hackathon).ExecuteCommandAsync(ct);

        if (req.GitHubRepositorySettings is not null)
        {
            if (HackathonGitHubRepositorySettingsMutation.ShouldPersist(gitHubRepositorySettings))
            {
                gitHubRepositorySettings!.HackathonId = hackathon.Id;
                var hasPersistedSettings = await sql.Queryable<HackathonGitHubRepositorySettings>()
                    .Where(s => s.HackathonId == hackathon.Id)
                    .AnyAsync(ct);

                if (hasPersistedSettings)
                {
                    await sql.Updateable(gitHubRepositorySettings).ExecuteCommandAsync(ct);
                }
                else
                {
                    await sql.Insertable(gitHubRepositorySettings).ExecuteCommandAsync(ct);
                }
            }
            else
            {
                await sql.Deleteable<HackathonGitHubRepositorySettings>()
                    .Where(s => s.HackathonId == hackathon.Id)
                    .ExecuteCommandAsync(ct);
                gitHubRepositorySettings = null;
            }
        }

        if (normalizedTemplates is not null)
        {
            await sql.Deleteable<HackathonNotificationTemplate>()
                .Where(t => t.ActivityId == hackathon.Id)
                .ExecuteCommandAsync(ct);

            if (normalizedTemplates.Count > 0)
            {
                var inserts = normalizedTemplates.Select(kvp => new HackathonNotificationTemplate
                {
                    Id = Guid.NewGuid(),
                    ActivityId = hackathon.Id,
                    EventKey = kvp.Key,
                    TemplateId = kvp.Value,
                });
                await sql.Insertable(inserts.ToList()).ExecuteCommandAsync(ct);
            }
        }

        var templates = normalizedTemplates ?? await LoadTemplates(hackathon.Id, ct);

        await Send.OkAsync(
            new Response
            {
                Id = hackathon.Id,
                Name = hackathon.Activity.Title,
                Description = hackathon.Activity.Description,
                Venue = hackathon.Activity.Location,
                HomepageUri = hackathon.HomepageUri,
                ShortCode = hackathon.ShortCode,
                IsPublished = hackathon.Activity.IsPublished,
                EventStartDate = hackathon.Activity.StartTime,
                EventEndDate = hackathon.Activity.EndTime,
                SubmissionsStartDate = hackathon.SubmissionsStartDate,
                ChallengeSelectionEndDate = hackathon.ChallengeSelectionEndDate,
                SubmissionsEndDate = hackathon.SubmissionsEndDate,
                JudgingStartDate = hackathon.JudgingStartDate,
                JudgingEndDate = hackathon.JudgingEndDate,
                EmailTemplates = templates,
                GitHubRepositorySettings = HackathonGitHubRepositorySettingsMapper.ToResponse(
                    gitHubRepositorySettings
                ),
            },
            ct
        );
    }

    private async Task<Dictionary<string, string>> LoadTemplates(Guid activityId, CancellationToken ct)
    {
        var persisted = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => t.ActivityId == activityId)
            .ToListAsync(ct);
        return persisted
            .GroupBy(t => t.EventKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().TemplateId, StringComparer.OrdinalIgnoreCase);
    }

    private void AddGitHubRepositorySettingsErrors(
        IReadOnlyList<HackathonGitHubRepositorySettingsValidationError> errors
    )
    {
        foreach (var error in errors)
        {
            switch (error.Field)
            {
                case HackathonGitHubRepositorySettingsField.ApiKey:
                    AddError(r => r.GitHubRepositorySettings!.ApiKey, error.Message);
                    break;
                case HackathonGitHubRepositorySettingsField.OrganizationId:
                    AddError(r => r.GitHubRepositorySettings!.OrganizationId, error.Message);
                    break;
            }
        }
    }
}
