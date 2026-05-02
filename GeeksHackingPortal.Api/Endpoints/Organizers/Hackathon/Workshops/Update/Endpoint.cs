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
        Description(b => b.WithTags("Workshops"));
        Summary(s =>
        {
            s.Summary = "Update a workshop";
            s.Description = "Updates an existing workshop.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var workshop = await sql.Queryable<Workshop>()
            .Includes(w => w.Activity)
            .FirstAsync(w => w.Id == req.WorkshopId && w.HackathonId == req.HackathonId, ct);

        if (workshop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var hasActivity = workshop.Activity is not null;

        workshop.MaxParticipants = req.MaxParticipants;

        using var tran = sql.Ado.UseTran();

        var activity = workshop.Activity ?? new Activity
        {
            Id = workshop.Id,
            Kind = ActivityKind.HackathonWorkshop,
        };
        activity.Title = req.Title;
        activity.Description = req.Description;
        activity.StartTime = req.StartTime;
        activity.EndTime = req.EndTime;
        activity.Location = req.Location;
        activity.IsPublished = req.IsPublished;
        activity.UpdatedAt = DateTimeOffset.UtcNow;

        if (hasActivity)
        {
            await sql.Updateable(activity).ExecuteCommandAsync(ct);
        }
        else
        {
            await sql.Insertable(activity).ExecuteCommandAsync(ct);
        }
        await sql.Updateable(workshop).ExecuteCommandAsync(ct);

        tran.CommitTran();

        await Send.OkAsync(
            new Response
            {
                Id = workshop.Id,
                Title = activity.Title,
                Description = activity.Description,
                StartTime = activity.StartTime,
                EndTime = activity.EndTime,
                Location = activity.Location,
                MaxParticipants = workshop.MaxParticipants,
                IsPublished = activity.IsPublished,
                UpdatedAt = activity.UpdatedAt,
            },
            ct
        );
    }
}
