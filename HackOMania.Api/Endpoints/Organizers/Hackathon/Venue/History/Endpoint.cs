using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Venue.History;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get(
            "organizers/hackathons/{HackathonId:guid}/participants/{ParticipantUserId:guid}/venue/history"
        );
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Venue"));
        Summary(s =>
        {
            s.Summary = "Get participant venue history";
            s.Description = "Returns check-in and check-out history for a participant.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var participantData = await sql.Queryable<Participant>()
            .LeftJoin<User>((p, u) => p.UserId == u.Id)
            .Where((p, u) => p.UserId == req.ParticipantUserId && p.HackathonId == req.HackathonId)
            .Select((p, u) => new { Participant = p, User = u })
            .FirstAsync(ct);

        if (participantData is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var history = await sql.Queryable<VenueCheckIn>()
            .Where(v => v.ParticipantId == participantData.Participant.Id)
            .OrderByDescending(v => v.CheckInTime)
            .ToListAsync(ct);

        await Send.OkAsync(
            new Response
            {
                ParticipantId = participantData.Participant.Id,
                UserId = participantData.Participant.UserId,
                UserName = participantData.User.FirstName + " " + participantData.User.LastName,
                IsCurrentlyCheckedIn = history.FirstOrDefault()?.IsCheckedIn ?? false,
                History =
                [
                    .. history.Select(v => new HistoryItemDto
                    {
                        CheckInTime = v.CheckInTime.AssumeStoredAsUtc(),
                        CheckOutTime = v.CheckOutTime.AssumeStoredAsUtc(),
                        IsCheckedIn = v.IsCheckedIn,
                    }),
                ],
            },
            ct
        );
    }
}
