using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.Create;

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
        var hackathon = await sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity).InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            ActivityId = hackathon.ActivityId,
            Name = req.Name,
            Description = req.Description ?? string.Empty,
            RedemptionStmt = req.RedemptionStmt,
            IsPublished = req.IsPublished,
        };

        await sql.Insertable(resource).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = resource.Id,
                HackathonId = hackathon.Id,
                Name = resource.Name,
                Description = resource.Description,
                RedemptionStmt = resource.RedemptionStmt,
                IsPublished = resource.IsPublished,
            },
            ct
        );
    }
}
