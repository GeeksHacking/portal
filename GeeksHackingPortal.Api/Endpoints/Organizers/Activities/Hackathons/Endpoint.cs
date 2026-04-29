using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Features.Hackathons.GitHubRepositorySettings;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Hackathons;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/activities/{ActivityId:guid}/hackathon");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Activities", "Hackathons"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.ActivityId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var gitHubRepositorySettings = await sql.Queryable<HackathonGitHubRepositorySettings>()
            .Where(s => s.HackathonId == hackathon.Id)
            .FirstAsync(ct);

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

        await Send.OkAsync(
            new Response
            {
                Id = hackathon.Id,
                SubmissionsStartDate = hackathon.SubmissionsStartDate,
                ChallengeSelectionEndDate = hackathon.ChallengeSelectionEndDate,
                SubmissionsEndDate = hackathon.SubmissionsEndDate,
                JudgingStartDate = hackathon.JudgingStartDate,
                JudgingEndDate = hackathon.JudgingEndDate,
                GitHubRepositorySettings = HackathonGitHubRepositorySettingsMapper.ToResponse(
                    gitHubRepositorySettings
                ),
            },
            ct
        );
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
