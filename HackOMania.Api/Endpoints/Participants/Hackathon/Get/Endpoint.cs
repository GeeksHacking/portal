using FastEndpoints;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonIdOrShortCode}");
        AllowAnonymous();
        Description(b => b.WithTags("Participants", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Get hackathon details";
            s.Description = "Retrieves public details about a hackathon by ID or short code.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var requestedToken = (req.HackathonIdOrShortCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(requestedToken))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var isGuidToken = Guid.TryParse(requestedToken, out var parsedHackathonId);
        var normalizedShortCode = requestedToken.ToLower();
        var cacheKeySegment = isGuidToken
            ? parsedHackathonId.ToString("D")
            : normalizedShortCode;
        var hackathonCacheKey = $"hackathon:public-details:{cacheKeySegment}";
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h =>
                (isGuidToken && h.Id == parsedHackathonId)
                || (
                    !isGuidToken
                    && h.ShortCode != null
                    && SqlFunc.ToLower(h.ShortCode) == normalizedShortCode
                )
            )
            .WithCache(hackathonCacheKey)
            .FirstAsync(ct);

        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

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
            },
            ct
        );
    }
}
