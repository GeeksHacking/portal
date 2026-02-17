using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Resources.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/resources");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Resources"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resources = await sql.Queryable<Resource>()
            .Where(r => r.HackathonId == hackathon.Id)
            .Select(r => new Response.Response_Resource
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                RedemptionStmt = r.RedemptionStmt,
            })
            .WithCache(3600) // Cache for 1 hour
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Resources = resources }, ct);
    }
}
