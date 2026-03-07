using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Resources.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/hackathons/{HackathonId:guid}/resources/{ResourceId}");
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

        var resource = await sql.Queryable<Resource>()
            .Where(r => r.Id.ToString() == req.ResourceId && r.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        if (resource is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            resource.Name = req.Name;
        }

        if (req.Description is not null)
        {
            resource.Description = req.Description;
        }

        if (req.RedemptionStmt is not null)
        {
            resource.RedemptionStmt = req.RedemptionStmt;
        }

        if (req.IsPublished.HasValue)
        {
            resource.IsPublished = req.IsPublished.Value;
        }

        await sql.Updateable(resource).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = resource.Id,
                HackathonId = resource.HackathonId,
                Name = resource.Name,
                Description = resource.Description,
                RedemptionStmt = resource.RedemptionStmt,
                IsPublished = resource.IsPublished,
            },
            ct
        );
    }
}
