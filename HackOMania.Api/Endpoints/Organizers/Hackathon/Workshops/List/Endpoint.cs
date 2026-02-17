using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Workshops.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/workshops");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Workshops"));
        Summary(s =>
        {
            s.Summary = "List workshops";
            s.Description = "Lists all workshops for the hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var workshops = await sql.Queryable<Workshop>()
            .Where(w => w.HackathonId == req.HackathonId)
            .Includes(w => w.Participants)
            .WithCache(1800) // Cache for 30 minutes
            .ToListAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Workshops = workshops
                    .Select(w => new WorkshopDto
                    {
                        Id = w.Id,
                        Title = w.Title,
                        Description = w.Description,
                        StartTime = w.StartTime,
                        EndTime = w.EndTime,
                        Location = w.Location,
                        MaxParticipants = w.MaxParticipants,
                        CurrentParticipants = w.Participants?.Count ?? 0,
                        IsPublished = w.IsPublished,
                    })
                    .ToList(),
            },
            ct
        );
    }
}
