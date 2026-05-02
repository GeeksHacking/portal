using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Judges.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/judges/{JudgeId}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Judges"));
        Summary(s =>
        {
            s.Summary = "Get judge details";
            s.Description = "Retrieves details about a specific judge including their secret.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Includes(h => h.Activity)
            
            .InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var judge = await sql.Queryable<Judge>()
            .Where(j => j.HackathonId == hackathon.Id && j.Id == req.JudgeId)
            
            .FirstAsync(ct);

        if (judge is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(
            new Response
            {
                Id = judge.Id,
                Name = judge.Name,
                Secret = judge.Secret,
                Active = judge.Active,
            },
            ct
        );
    }
}
