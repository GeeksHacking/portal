using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/hackathons/{HackathonId:guid}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Update hackathon details";
            s.Description = "Updates the hackathon information.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            hackathon.Name = req.Name;
        }

        if (!string.IsNullOrWhiteSpace(req.Description))
        {
            hackathon.Description = req.Description;
        }

        if (!string.IsNullOrWhiteSpace(req.Venue))
        {
            hackathon.Venue = req.Venue;
        }

        if (req.HomepageUri is not null)
        {
            hackathon.HomepageUri = req.HomepageUri;
        }

        if (!string.IsNullOrWhiteSpace(req.ShortCode))
        {
            hackathon.ShortCode = req.ShortCode;
        }

        if (req.EventStartDate.HasValue)
        {
            hackathon.EventStartDate = req.EventStartDate.Value;
        }

        if (req.EventEndDate.HasValue)
        {
            hackathon.EventEndDate = req.EventEndDate.Value;
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

        if (req.IsPublished.HasValue)
        {
            hackathon.IsPublished = req.IsPublished.Value;
        }

        var emailTemplates = req.EmailTemplates is null
            ? null
            : NormalizeEmailTemplates(req.EmailTemplates);

        if (emailTemplates is not null)
        {
            await sql.Deleteable<HackathonNotificationTemplate>()
                .Where(t => t.HackathonId == hackathon.Id)
                .ExecuteCommandAsync(ct);

            if (emailTemplates.Count > 0)
            {
                var notificationTemplates = emailTemplates.Select(
                    kvp => new HackathonNotificationTemplate
                    {
                        Id = Guid.NewGuid(),
                        HackathonId = hackathon.Id,
                        EventKey = kvp.Key,
                        TemplateId = kvp.Value,
                    }
                );

                await sql.Insertable(notificationTemplates.ToList()).ExecuteCommandAsync(ct);
            }
        }

        await sql.Updateable(hackathon).ExecuteCommandAsync(ct);

        var persistedTemplates = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => t.HackathonId == hackathon.Id)
            .ToListAsync(ct);
        var emailTemplateMap = persistedTemplates
            .GroupBy(t => t.EventKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().TemplateId, StringComparer.OrdinalIgnoreCase);

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
            },
            ct
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
}
