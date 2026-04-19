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
        Description(b => b.WithTags("Organizers", "Workshops"));
        Summary(s =>
        {
            s.Summary = "Create a workshop";
            s.Description = "Creates a new workshop for the hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathonId = req.HackathonId;

        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            Title = req.Title,
            Description = req.Description,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            Location = req.Location,
            MaxParticipants = req.MaxParticipants,
            IsPublished = req.IsPublished,
        };

        await sql.Insertable(workshop).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = workshop.Id,
                HackathonId = workshop.HackathonId,
                Title = workshop.Title,
                Description = workshop.Description,
                StartTime = workshop.StartTime,
                EndTime = workshop.EndTime,
                Location = workshop.Location,
                MaxParticipants = workshop.MaxParticipants,
                IsPublished = workshop.IsPublished,
                CreatedAt = workshop.CreatedAt,
            },
            ct
        );
    }
}
