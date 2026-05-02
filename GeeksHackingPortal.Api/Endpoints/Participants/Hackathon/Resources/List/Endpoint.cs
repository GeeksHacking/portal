using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Resources.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId:guid}/resources");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Resources"));
        Summary(s =>
        {
            s.Summary = "List hackathon resources";
            s.Description = "Retrieves all resources available for a hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity).InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.Activity.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resources = await sql.Queryable<Resource>()
            .Where(r => r.ActivityId == hackathon.Id && r.IsPublished)
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
