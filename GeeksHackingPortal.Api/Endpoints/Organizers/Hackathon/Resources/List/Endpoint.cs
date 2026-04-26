using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.List;

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
        var hackathon = await sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity).InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resources = await sql.Queryable<Resource>()
            .Where(r => r.ActivityId == hackathon.Id)
            .Select(r => new Response.Response_Resource
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                RedemptionStmt = r.RedemptionStmt,
                IsPublished = r.IsPublished,
            })
            
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Resources = resources }, ct);
    }
}
