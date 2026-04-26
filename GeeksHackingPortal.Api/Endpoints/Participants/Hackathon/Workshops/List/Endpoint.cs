using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Workshops.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId:guid}/workshops");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Workshops"));
        Summary(s =>
        {
            s.Summary = "List workshops";
            s.Description = "Lists all published workshops for the hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var hackathonId = req.HackathonId;

        var participant = await sql.Queryable<Participant>()
            .FirstAsync(p => p.UserId == userId && p.HackathonId == hackathonId, ct);

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var workshops = await sql.Queryable<Workshop>()
            .Where(w => w.HackathonId == hackathonId)
            .Includes(w => w.Activity)
            .Includes(w => w.Participants)
            
            .ToListAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Workshops = workshops
                    .Where(w => w.Activity.IsPublished)
                    .Select(w =>
                    {
                        var joined = w.Participants?.FirstOrDefault(wp =>
                            wp.ParticipantId == participant.Id
                        );
                        return new WorkshopDto
                        {
                            Id = w.Id,
                            Title = w.Activity.Title,
                            Description = w.Activity.Description,
                            StartTime = w.Activity.StartTime,
                            EndTime = w.Activity.EndTime,
                            Location = w.Activity.Location,
                            MaxParticipants = w.MaxParticipants,
                            CurrentParticipants = w.Participants?.Count ?? 0,
                            IsJoined = joined is not null,
                            HasAttended = joined?.HasAttended ?? false,
                        };
                    })
                    .ToList(),
            },
            ct
        );
    }
}
