using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Workshops.List;

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
            .Where(w => w.HackathonId == hackathonId && w.IsPublished)
            .Includes(w => w.Participants)
            .WithCache()
            .ToListAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Workshops = workshops
                    .Select(w =>
                    {
                        var joined = w.Participants?.FirstOrDefault(wp =>
                            wp.ParticipantId == participant.Id
                        );
                        return new WorkshopDto
                        {
                            Id = w.Id,
                            Title = w.Title,
                            Description = w.Description,
                            StartTime = w.StartTime,
                            EndTime = w.EndTime,
                            Location = w.Location,
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
