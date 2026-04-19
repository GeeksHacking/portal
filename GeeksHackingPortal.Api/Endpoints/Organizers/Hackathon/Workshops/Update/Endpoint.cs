using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Workshops.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Put("organizers/hackathons/{HackathonId:guid}/workshops/{WorkshopId:guid}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Workshops"));
        Summary(s =>
        {
            s.Summary = "Update a workshop";
            s.Description = "Updates an existing workshop.";
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

        workshop.Title = req.Title;
        workshop.Description = req.Description;
        workshop.StartTime = req.StartTime;
        workshop.EndTime = req.EndTime;
        workshop.Location = req.Location;
        workshop.MaxParticipants = req.MaxParticipants;
        workshop.IsPublished = req.IsPublished;
        workshop.UpdatedAt = DateTimeOffset.UtcNow;

        await sql.Updateable(workshop).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = workshop.Id,
                Title = workshop.Title,
                Description = workshop.Description,
                StartTime = workshop.StartTime,
                EndTime = workshop.EndTime,
                Location = workshop.Location,
                MaxParticipants = workshop.MaxParticipants,
                IsPublished = workshop.IsPublished,
                UpdatedAt = workshop.UpdatedAt,
            },
            ct
        );
    }
}
