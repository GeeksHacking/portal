using FastEndpoints;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Status;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId:guid}/status");
        Description(b => b.WithTags("Participants"));
        Summary(s =>
        {
            s.Summary = "Get my participation status";
            s.Description =
                "Retrieves the current user's participation status for a hackathon, including team info and review status.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var currentUserId = User.GetUserId();

        var isOrganizer = await sql.Queryable<Organizer>()
            .AnyAsync(o => o.HackathonId == hackathon.Id && o.UserId == currentUserId, ct);

        var participant = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == currentUserId)
            .FirstAsync(ct);

        if (participant is null)
        {
            await Send.OkAsync(
                new Response
                {
                    IsParticipant = false,
                    IsOrganizer = isOrganizer,
                    TeamId = null,
                    TeamName = null,
                    Status = null,
                },
                ct
            );
            return;
        }

        string? teamName = null;
        if (participant.TeamId.HasValue)
        {
            var team = await sql.Queryable<Team>()
                .Where(t => t.Id == participant.TeamId.Value)
                .FirstAsync(ct);
            teamName = team?.Name;
        }

        var latestReview = await sql.Queryable<ParticipantReview>()
            .Where(r => r.ParticipantId == participant.UserId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstAsync(ct);

        var status = latestReview switch
        {
            { Status: ParticipantReview.ParticipantReviewStatus.Accepted } =>
                ParticipantStatus.Accepted,
            { Status: ParticipantReview.ParticipantReviewStatus.Rejected } =>
                ParticipantStatus.Rejected,
            _ => ParticipantStatus.Pending,
        };

        await Send.OkAsync(
            new Response
            {
                IsParticipant = true,
                IsOrganizer = isOrganizer,
                TeamId = participant.TeamId,
                TeamName = teamName,
                Status = status,
            },
            ct
        );
    }
}
