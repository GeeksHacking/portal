using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Workshops.Delete;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("organizers/hackathons/{HackathonId:guid}/workshops/{WorkshopId:guid}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Workshops"));
        Summary(s =>
        {
            s.Summary = "Delete a workshop";
            s.Description = "Deletes an existing workshop.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var workshop = await sql.Queryable<Workshop>()
            .FirstAsync(w => w.Id == req.WorkshopId && w.HackathonId == req.HackathonId, ct);

        if (workshop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Delete all participants first
        await sql.Deleteable<WorkshopParticipant>()
            .Where(wp => wp.WorkshopId == req.WorkshopId)
            .ExecuteCommandAsync(ct);

        await sql.Deleteable(workshop).ExecuteCommandAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
