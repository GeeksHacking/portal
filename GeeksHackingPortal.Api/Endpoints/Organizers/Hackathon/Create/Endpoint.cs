using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Features.Hackathons.GitHubRepositorySettings;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons");
        Policies(PolicyNames.CreateHackathon);
        Description(b => b.WithTags("Organizers", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Create a new hackathon";
            s.Description =
                "Creates a new hackathon event. Permissions depend on the platform configuration.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var emailTemplates = NormalizeEmailTemplates(req.EmailTemplates);
        var challengeSelectionEndDate = req.ChallengeSelectionEndDate ?? req.SubmissionsEndDate;
        var gitHubRepositorySettingsResult = HackathonGitHubRepositorySettingsMutation.BuildForCreate(
            req.GitHubRepositorySettings
        );
        AddGitHubRepositorySettingsErrors(gitHubRepositorySettingsResult.Errors);
        if (ValidationFailed)
        {
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var hackathonId = Guid.NewGuid();
        var activity = new Activity
        {
            Id = hackathonId,
            Kind = ActivityKind.Hackathon,
            Title = req.Name,
            Description = req.Description,
            Location = req.Venue,
            StartTime = req.EventStartDate,
            EndTime = req.EventEndDate,
            IsPublished = req.IsPublished,
        };
        var organizer = new Organizer
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            UserId = userId.Value,
            Type = OrganizerType.Admin,
        };
        var activityOrganizer = new ActivityOrganizer
        {
            Id = organizer.Id,
            ActivityId = activity.Id,
            UserId = userId.Value,
            Type = organizer.Type,
        };

        var hackathon = new Entities.Hackathon
        {
            Id = hackathonId,
            Activity = activity,
            HomepageUri = req.HomepageUri,
            ShortCode = req.ShortCode,
            Name = req.Name,
            Description = req.Description,
            Venue = req.Venue,
            IsPublished = req.IsPublished,
            EventStartDate = req.EventStartDate,
            EventEndDate = req.EventEndDate,
            SubmissionsStartDate = req.SubmissionsStartDate,
            ChallengeSelectionEndDate = challengeSelectionEndDate,
            SubmissionsEndDate = req.SubmissionsEndDate,
            JudgingStartDate = req.JudgingStartDate,
            JudgingEndDate = req.JudgingEndDate,
            Organizers = [organizer],
        };

        var transactionResult = await sql.Ado.UseTranAsync(async () =>
        {
            await sql.Insertable(activity).ExecuteCommandAsync(ct);
            await sql.Insertable(hackathon).ExecuteCommandAsync(ct);
            await sql.Insertable(hackathon.Organizers).ExecuteCommandAsync(ct);
            await sql.Insertable(activityOrganizer).ExecuteCommandAsync(ct);
        });

        if (!transactionResult.IsSuccess)
        {
            throw transactionResult.ErrorException!;
        }

        if (HackathonGitHubRepositorySettingsMutation.ShouldPersist(gitHubRepositorySettingsResult.Settings))
        {
            gitHubRepositorySettingsResult.Settings!.HackathonId = hackathon.Id;
            await sql.Insertable(gitHubRepositorySettingsResult.Settings).ExecuteCommandAsync(ct);
        }

        if (emailTemplates.Count > 0)
        {
            var notificationTemplates = emailTemplates.Select(
                kvp => new HackathonNotificationTemplate
                {
                    Id = Guid.NewGuid(),
                    ActivityId = hackathon.Id,
                    EventKey = kvp.Key,
                    TemplateId = kvp.Value,
                }
            );

            await sql.Insertable(notificationTemplates.ToList()).ExecuteCommandAsync(ct);
        }

        await Send.CreatedAtAsync<Get.Endpoint>(
            new { HackathonId = hackathon.Id },
            new Response
            {
                Id = hackathon.Id,
                Name = hackathon.Name,
                Description = hackathon.Description,
                Venue = hackathon.Venue,
                HomepageUri = hackathon.HomepageUri,
                ShortCode = hackathon.ShortCode,
                IsPublished = hackathon.IsPublished,
                EventStartDate = hackathon.EventStartDate,
                EventEndDate = hackathon.EventEndDate,
                SubmissionsStartDate = hackathon.SubmissionsStartDate,
                ChallengeSelectionEndDate = hackathon.ChallengeSelectionEndDate,
                SubmissionsEndDate = hackathon.SubmissionsEndDate,
                JudgingStartDate = hackathon.JudgingStartDate,
                JudgingEndDate = hackathon.JudgingEndDate,
                EmailTemplates = emailTemplates,
                GitHubRepositorySettings = HackathonGitHubRepositorySettingsMapper.ToResponse(
                    gitHubRepositorySettingsResult.Settings
                ),
            },
            cancellation: ct
        );
    }

    private static Dictionary<string, string> NormalizeEmailTemplates(
        Dictionary<string, string>? templates
    )
    {
        if (templates is null)
        {
            return [];
        }

        return templates
            .Where(kvp =>
                !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value)
            )
            .GroupBy(kvp => kvp.Key.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().Value.Trim(), StringComparer.OrdinalIgnoreCase);
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
