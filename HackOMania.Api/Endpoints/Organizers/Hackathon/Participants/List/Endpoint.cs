using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/participants");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Participants"));
        Summary(s =>
        {
            s.Summary = "List all participants";
            s.Description = "Retrieves all participants for a hackathon with their review status.";
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

        var participants = await sql.Queryable<Participant>()
            .Includes(p => p.ParticipantReviews)
            .Where(p => p.HackathonId == hackathon.Id)
            .OrderByDescending(p => p.JoinedAt)
            .ToListAsync(ct);

        var teamIds = participants
            .Where(p => p.TeamId.HasValue)
            .Select(p => p.TeamId!.Value)
            .Distinct()
            .ToList();

        var teams = await sql.Queryable<Team>()
            .Where(t => teamIds.Contains(t.Id))
            .ToListAsync(ct)
            .ContinueWith(t => t.Result.ToDictionary(x => x.Id, x => x.Name), ct);

        var participantResponses = participants
            .Select(p =>
            {
                var concludedStatus = p.ConcludedStatus switch
                {
                    ParticipantReview.ParticipantReviewStatus.Accepted =>
                        ParticipantConcludedStatus.Accepted,
                    ParticipantReview.ParticipantReviewStatus.Rejected =>
                        ParticipantConcludedStatus.Rejected,
                    _ => ParticipantConcludedStatus.Pending,
                };

                return new ParticipantItem
                {
                    Id = p.UserId,
                    TeamId = p.TeamId,
                    TeamName = p.TeamId.HasValue ? teams.GetValueOrDefault(p.TeamId.Value) : null,
                    ConcludedStatus = concludedStatus,
                    Reviews =
                    [
                        .. p.ParticipantReviews.Select(r => new ParticipantReviewItem
                        {
                            Id = r.Id,
                            Status = r.Status switch
                            {
                                ParticipantReview.ParticipantReviewStatus.Accepted =>
                                    ParticipantReviewItem.ParticipantReviewStatus.Accepted,
                                ParticipantReview.ParticipantReviewStatus.Rejected =>
                                    ParticipantReviewItem.ParticipantReviewStatus.Rejected,
                                _ => throw new ArgumentOutOfRangeException(),
                            },
                            Reason = r.Reason,
                            CreatedAt = r.CreatedAt,
                        }),
                    ],
                };
            })
            .ToList();

        await Send.OkAsync(
            new Response
            {
                Participants = participantResponses,
                TotalCount = participants.Count,
                PendingCount = participantResponses.Count(p =>
                    p.ConcludedStatus == ParticipantConcludedStatus.Pending
                ),
                AcceptedCount = participantResponses.Count(p =>
                    p.ConcludedStatus == ParticipantConcludedStatus.Accepted
                ),
                RejectedCount = participantResponses.Count(p =>
                    p.ConcludedStatus == ParticipantConcludedStatus.Rejected
                ),
            },
            ct
        );
    }
}
