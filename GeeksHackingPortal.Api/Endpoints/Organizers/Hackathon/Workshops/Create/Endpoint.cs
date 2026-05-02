using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Workshops.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId:guid}/workshops");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Workshops"));
        Summary(s =>
        {
            s.Summary = "Create a workshop";
            s.Description = "Creates a new workshop for the hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathonId = req.HackathonId;
        var workshopId = Guid.NewGuid();
        var activity = new Activity
        {
            Id = workshopId,
            Kind = ActivityKind.HackathonWorkshop,
            Title = req.Title,
            Description = req.Description,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            Location = req.Location,
            IsPublished = req.IsPublished,
        };

        var workshop = new Workshop
        {
            Id = workshopId,
            HackathonId = hackathonId,
            Activity = activity,
            MaxParticipants = req.MaxParticipants,
        };

        var transactionResult = await sql.Ado.UseTranAsync(async () =>
        {
            await sql.Insertable(activity).ExecuteCommandAsync(ct);
            await sql.Insertable(workshop).ExecuteCommandAsync(ct);
        });

        if (!transactionResult.IsSuccess)
        {
            throw transactionResult.ErrorException!;
        }

        await Send.OkAsync(
            new Response
            {
                Id = workshop.Id,
                HackathonId = workshop.HackathonId,
                Title = activity.Title,
                Description = activity.Description,
                StartTime = activity.StartTime,
                EndTime = activity.EndTime,
                Location = activity.Location,
                MaxParticipants = workshop.MaxParticipants,
                IsPublished = activity.IsPublished,
                CreatedAt = activity.CreatedAt,
            },
            ct
        );
    }
}
