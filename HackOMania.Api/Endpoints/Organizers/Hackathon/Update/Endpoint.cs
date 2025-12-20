using FastEndpoints;
using HackOMania.Api.Authorization;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/hackathons/{HackathonId}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Update hackathon details";
            s.Description = "Updates the hackathon information. Requires organizer access.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h => h.Id == req.HackathonId)
            .FirstAsync(ct);

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
            hackathon.HomepageUri = req.HomepageUri.ToString();
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

        await sql.Updateable(hackathon).ExecuteCommandAsync(ct);

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
                SubmissionsEndDate = hackathon.SubmissionsEndDate,
                JudgingStartDate = hackathon.JudgingStartDate,
                JudgingEndDate = hackathon.JudgingEndDate,
            },
            ct
        );
    }
}
