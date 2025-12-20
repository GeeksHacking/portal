using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId}/participants");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Participants"));
        Summary(s =>
        {
            s.Summary = "List all participants (Organizer)";
            s.Description =
                "Retrieves all participants for a hackathon with their review status. Requires organizer access.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h => h.Id == req.HackathonId)
            .FirstAsync(ct);

        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Get all participants with their latest review status
        var participants = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id)
            .OrderByDescending(p => p.JoinedAt)
            .ToListAsync(ct);

        var participantIds = participants.Select(p => p.UserId).ToList();

        // Get user info
        var users = await sql.Queryable<User>()
            .Where(u => participantIds.Contains(u.Id))
            .ToListAsync(ct)
            .ContinueWith(t => t.Result.ToDictionary(u => u.Id));

        // Get latest reviews for each participant
        var reviews = await sql.Queryable<ParticipantReview>()
            .Where(r => participantIds.Contains(r.ParticipantId))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var latestReviews = reviews
            .GroupBy(r => r.ParticipantId)
            .ToDictionary(g => g.Key, g => g.First());

        // Get team info
        var teamIds = participants
            .Where(p => p.TeamId.HasValue)
            .Select(p => p.TeamId!.Value)
            .Distinct()
            .ToList();

        var teams = await sql.Queryable<Team>()
            .Where(t => teamIds.Contains(t.Id))
            .ToListAsync(ct)
            .ContinueWith(t => t.Result.ToDictionary(x => x.Id, x => x.Name));

        var participantResponses = participants
            .Select(p =>
            {
                var user = users.GetValueOrDefault(p.UserId);
                var review = latestReviews.GetValueOrDefault(p.UserId);
                var status = review switch
                {
                    { Status: ParticipantReview.ParticipantReviewStatus.Accepted } =>
                        ParticipantStatus.Accepted,
                    { Status: ParticipantReview.ParticipantReviewStatus.Rejected } =>
                        ParticipantStatus.Rejected,
                    _ => ParticipantStatus.Pending,
                };

                return new ParticipantItem
                {
                    UserId = p.UserId,
                    TeamId = p.TeamId,
                    TeamName = p.TeamId.HasValue ? teams.GetValueOrDefault(p.TeamId.Value) : null,
                    Status = status,
                };
            })
            .ToList();

        // Apply status filter if provided
        if (!string.IsNullOrWhiteSpace(req.Status))
        {
            var filterStatus = req.Status.ToLowerInvariant() switch
            {
                "pending" => ParticipantStatus.Pending,
                "accepted" => ParticipantStatus.Accepted,
                "rejected" => ParticipantStatus.Rejected,
                _ => (ParticipantStatus?)null,
            };

            if (filterStatus.HasValue)
            {
                participantResponses = participantResponses
                    .Where(p => p.Status == filterStatus.Value)
                    .ToList();
            }
        }

        await Send.OkAsync(
            new Response
            {
                Participants = participantResponses,
                TotalCount = participants.Count,
                PendingCount = participantResponses.Count(p =>
                    p.Status == ParticipantStatus.Pending
                ),
                AcceptedCount = participantResponses.Count(p =>
                    p.Status == ParticipantStatus.Accepted
                ),
                RejectedCount = participantResponses.Count(p =>
                    p.Status == ParticipantStatus.Rejected
                ),
            },
            ct
        );
    }
}
