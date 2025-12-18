using FastEndpoints;
using HackOMania.Api.Authorization;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Judges.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{Id}/judges/{JudgeId}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Judges"));
        Summary(s =>
        {
            s.Summary = "Get judge details (Organizer)";
            s.Description =
                "Retrieves details about a specific judge including their secret. Requires organizer access.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h => h.Id.ToString() == req.Id || h.ShortCode == req.Id)
            .FirstAsync(ct);

        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var judge = await sql.Queryable<Entities.Judge>()
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
