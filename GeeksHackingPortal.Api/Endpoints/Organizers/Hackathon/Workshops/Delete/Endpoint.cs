using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Storage;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Workshops.Delete;

public class Endpoint(ISqlSugarClient sql, ActivityFileStorage storage) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("organizers/hackathons/{HackathonId:guid}/workshops/{WorkshopId:guid}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Workshops"));
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

        var files = await sql.Queryable<ActivityFile>()
            .Where(f => f.ActivityId == workshop.Id)
            .ToListAsync(ct);

        var transactionResult = await sql.Ado.UseTranAsync(async () =>
        {
            await sql.Deleteable<ActivityFile>()
                .Where(f => f.ActivityId == workshop.Id)
                .ExecuteCommandAsync(ct);

            await sql.Deleteable<WorkshopParticipant>()
                .Where(wp => wp.WorkshopId == req.WorkshopId)
                .ExecuteCommandAsync(ct);

            await sql.Deleteable(workshop).ExecuteCommandAsync(ct);
            await sql.Deleteable<Activity>()
                .Where(activity => activity.Id == workshop.Id)
                .ExecuteCommandAsync(ct);
        });

        if (!transactionResult.IsSuccess)
        {
            throw transactionResult.ErrorException!;
        }

        await storage.DeleteObjectsAsync(files.Select(f => f.StorageKey));

        await Send.NoContentAsync(ct);
    }
}
