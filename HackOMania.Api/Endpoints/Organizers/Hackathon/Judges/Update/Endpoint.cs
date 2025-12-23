using FastEndpoints;
using HackOMania.Api.Authorization;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Judges.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/hackathons/{HackathonId:guid}/judges/{JudgeId}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Judges"));
        Summary(s =>
        {
            s.Summary = "Update a judge";
            s.Description =
                "Updates judge details. Can regenerate the secret to invalidate old links.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
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

        if (req.Name is not null)
        {
            judge.Name = req.Name;
        }

        if (req.Active.HasValue)
        {
            judge.Active = req.Active.Value;
        }

        if (req.RegenerateSecret)
        {
            judge.Secret = Guid.NewGuid();
        }

        await sql.Updateable(judge).ExecuteCommandAsync(ct);

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
