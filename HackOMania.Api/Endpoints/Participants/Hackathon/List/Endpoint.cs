using FastEndpoints;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.List;

public class Endpoint(ISqlSugarClient sql, IHackathonCacheService cacheService)
    : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("participants/hackathons");
        AllowAnonymous();
        Description(b => b.WithTags("Participants", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "List all hackathons";
            s.Description = "Retrieves all published hackathons.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var cacheKey = cacheService.GetParticipantListCacheKey();
        var cachedResponse = await cacheService.GetAsync<Response>(cacheKey, ct);

        if (cachedResponse is not null)
        {
            await Send.OkAsync(cachedResponse, ct);
            return;
        }

        var hackathons = await sql.Queryable<Entities.Hackathon>()
            .Where(h => h.IsPublished)
            .ToListAsync(ct);

        var response = new Response
        {
            Hackathons = hackathons.Select(h => new Response.HackathonItem
            {
                Id = h.Id,
                Name = h.Name,
                Description = h.Description,
                Venue = h.Venue,
                HomepageUri = h.HomepageUri,
                ShortCode = h.ShortCode,
                IsPublished = h.IsPublished,
                EventStartDate = h.EventStartDate,
                EventEndDate = h.EventEndDate,
                SubmissionsStartDate = h.SubmissionsStartDate,
                SubmissionsEndDate = h.SubmissionsEndDate,
                JudgingStartDate = h.JudgingStartDate,
                JudgingEndDate = h.JudgingEndDate,
            }),
        };

        await cacheService.SetAsync(cacheKey, response, ct: ct);
        await Send.OkAsync(response, ct);
    }
}
