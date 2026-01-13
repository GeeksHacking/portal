using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Workshops.Delete;

public class Endpoint(ISqlSugarClient sql) : EndpointWithoutRequest
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

    public override async Task HandleAsync(CancellationToken ct)
    {
        var hackathonId = Route<Guid>("HackathonId");
        var workshopId = Route<Guid>("WorkshopId");

        var workshop = await sql.Queryable<Workshop>()
            .FirstAsync(w => w.Id == workshopId && w.HackathonId == hackathonId, ct);

        if (workshop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Delete all participants first
        await sql.Deleteable<WorkshopParticipant>()
            .Where(wp => wp.WorkshopId == workshopId)
            .ExecuteCommandAsync(ct);

        await sql.Deleteable(workshop).ExecuteCommandAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
