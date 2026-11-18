using FastEndpoints;
using GeeksHackingPortal.Api.Features.ActivityFiles;
using GeeksHackingPortal.Api.Storage;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Get;

public class Endpoint(ISqlSugarClient sql, ActivityFileStorage storage) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonIdOrShortCode}");
        AllowAnonymous();
        Description(b => b.WithTags("Hackathons"));
        Summary(s =>
        {
            s.Summary = "Get hackathon details";
            s.Description = "Retrieves public details about a hackathon by ID or short code.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h =>
                h.Id.ToString() == req.HackathonIdOrShortCode
                || h.ShortCode == req.HackathonIdOrShortCode
            )
            .Includes(h => h.Activity)
            
            .FirstAsync(ct);

        if (hackathon is null || !hackathon.Activity.IsPublished || hackathon.Activity.EndTime <= DateTimeOffset.UtcNow)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

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
                BannerUrl = await ActivityBanners.GetUrlAsync(sql, storage, hackathon.Id, ct),
            },
            ct
        );
    }
}
