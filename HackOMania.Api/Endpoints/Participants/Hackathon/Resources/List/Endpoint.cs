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
        Get("participants/hackathons/{Id}/resources");
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
        var id = req.Id;
        if (string.IsNullOrWhiteSpace(id))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var hackathon = await membership.FindHackathon(id, ct);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resources = await sql.Queryable<Entities.Resource>()
            .Where(r => r.HackathonId == hackathon.Id)
            .Select(r => new Response.Response_Resource
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
            })
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Resources = resources }, ct);
    }
}
