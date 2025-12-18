using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Judges.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{Id}/judges");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Judges"));
        Summary(s =>
        {
            s.Summary = "Create a judge (Organizer)";
            s.Description =
                "Creates a new judge for the hackathon. Returns the judge secret that should be shared with the judge for authentication.";
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

        var judge = new Judge
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Secret = Guid.NewGuid(),
            Active = true,
            HackathonId = hackathon.Id,
        };

        await sql.Insertable(judge).ExecuteCommandAsync(ct);

        await Send.CreatedAtAsync<Get.Endpoint>(
            new { Id = req.Id, JudgeId = judge.Id },
            new Response
            {
                Id = judge.Id,
                Name = judge.Name,
                Secret = judge.Secret,
                Active = judge.Active,
            },
            cancellation: ct
        );
    }
}
