using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Judges.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/judges");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Judges"));
        Summary(s =>
        {
            s.Summary = "List all judges";
            s.Description = "Retrieves all judges for a hackathon including their secrets.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity).InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var judges = await sql.Queryable<Judge>()
            .Where(j => j.HackathonId == hackathon.Id)
            .Select(j => new JudgeItem
            {
                Id = j.Id,
                Name = j.Name,
                Secret = j.Secret,
                Active = j.Active,
            })
            
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Judges = judges }, ct);
    }
}
