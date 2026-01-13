using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Resources.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId:guid}/resources");
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

        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathon.Id,
            Name = req.Name,
            Description = req.Description ?? string.Empty,
            RedemptionStmt = req.RedemptionStmt,
        };

        await sql.Insertable(resource).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = resource.Id,
                HackathonId = resource.HackathonId,
                Name = resource.Name,
                Description = resource.Description,
                RedemptionStmt = resource.RedemptionStmt,
            },
            ct
        );
    }
}
