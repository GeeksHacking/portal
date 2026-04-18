using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Features.Hackathons.GitHubRepositorySettings;
using SqlSugar;
using HackathonEntity = HackOMania.Api.Entities.Hackathon;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Get hackathon details";
            s.Description = "Retrieves detailed information about a specific hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathonCacheKey = $"hackathon:details:{req.HackathonId}";
        var hackathon = await sql.Queryable<HackathonEntity>()
            .Where(h => h.Id == req.HackathonId)
            .WithCache(hackathonCacheKey)
            .FirstAsync(ct);

        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var emailTemplates = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => t.HackathonId == hackathon.Id)
            .WithCache($"hackathon:details:{hackathon.Id}:notification-templates")
            .ToListAsync(ct);
        var emailTemplateMap = emailTemplates
            .GroupBy(t => t.EventKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().TemplateId, StringComparer.OrdinalIgnoreCase);
        var gitHubRepositorySettings = await sql.Queryable<HackathonGitHubRepositorySettings>()
            .Where(s => s.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        await Send.OkAsync(
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
                EmailTemplates = emailTemplateMap,
                GitHubRepositorySettings = HackathonGitHubRepositorySettingsMapper.ToResponse(
                    gitHubRepositorySettings
                ),
            },
            ct
        );
    }
}
