using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Resources.List;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId:guid}/resources");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Resources"));
        Summary(s =>
        {
            s.Summary = "List hackathon resources";
            s.Description = "Retrieves all resources available for a hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resources = await sql.Queryable<Entities.Resource>()
            .Where(r => r.HackathonId == hackathon.Id)
            .Select(r => new Response.ResourceItem
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
            })
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Resources = resources }, ct);
    }
}
