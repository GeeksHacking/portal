using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Participants.List;

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
        var hackathonExists = await sql.Queryable<Entities.Hackathon>()
            .WithCache()
            .AnyAsync(h => h.Id == req.HackathonId, ct);
        if (!hackathonExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var participants = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == req.HackathonId)
            .OrderByDescending(p => p.JoinedAt)
            .WithCache()
            .Select(p => new Participant
            {
                Id = p.Id,
                UserId = p.UserId,
                TeamId = p.TeamId,
                JoinedAt = p.JoinedAt,
                WithdrawnAt = p.WithdrawnAt,
            })
            .ToListAsync(ct);

        if (participants.Count == 0)
        {
            await Send.OkAsync(
                new Response
                {
                    Participants = [],
                    TotalCount = 0,
                    PendingCount = 0,
                    AcceptedCount = 0,
                    RejectedCount = 0,
                },
                ct
            );
            return;
        }

        var userIds = participants.Select(p => p.UserId).Distinct().ToList();
        var participantIds = participants.Select(p => p.Id).Distinct().ToList();

        var usersList = await sql.Queryable<User>()
            .Where(u => userIds.Contains(u.Id))
            .WithCache()
            .ToListAsync(ct);
        var users = usersList.ToDictionary(x => x.Id, x => x);

        var teamIds = participants
            .Where(p => p.TeamId.HasValue)
            .Select(p => p.TeamId!.Value)
            .Distinct()
            .ToList();

        var teamsList =
            teamIds.Count == 0
                ? []
                : await sql.Queryable<Team>()
                    .Where(t => teamIds.Contains(t.Id))
                    .WithCache()
                    .ToListAsync(ct);
        var teams = teamsList.ToDictionary(x => x.Id, x => x.Name);

        // NOTE: ParticipantReview cache may be invalidated frequently during registration review periods.
        // See CACHING.md for details. This is generally acceptable as reviews happen in bursts.
        var reviewsList = await sql.Queryable<ParticipantReview>()
            .Where(r => participantIds.Contains(r.ParticipantId))
            .OrderByDescending(r => r.CreatedAt)
            .WithCache()
            .ToListAsync(ct);

        var reviewsByParticipant = reviewsList
            .GroupBy(r => r.ParticipantId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAt).ToList());

        var submissionList = await sql.Queryable<ParticipantRegistrationSubmission>()
            .LeftJoin<RegistrationQuestion>((s, q) => s.QuestionId == q.Id)
            .Where((s, q) => participantIds.Contains(s.ParticipantId))
            .WithCache()
            .Select(
                (s, q) =>
                    new
                    {
                        s.ParticipantId,
                        Item = new RegistrationSubmissionItem
                        {
                            QuestionId = s.QuestionId,
                            QuestionText = q.QuestionText,
                            Value = s.Value,
                            FollowUpValue = s.FollowUpValue,
                            UpdatedAt = s.UpdatedAt,
                        },
                    }
            )
            .ToListAsync(ct);

        var submissionsByParticipant = submissionList
            .GroupBy(s => s.ParticipantId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Item).ToList());

        var emailDeliveries = await sql.Queryable<ParticipantEmailDelivery>()
            .Where(e => participantIds.Contains(e.ParticipantId))
            .OrderByDescending(e => e.SentAt)
            .WithCache()
            .ToListAsync(ct);
        var emailDeliveriesByParticipant = emailDeliveries
            .GroupBy(e => e.ParticipantId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var participantResponses = participants
            .Select(p =>
            {
                var userInfo = users.GetValueOrDefault(p.UserId);
                var reviews = reviewsByParticipant.GetValueOrDefault(p.Id) ?? [];
                var deliveryLogs = emailDeliveriesByParticipant.GetValueOrDefault(p.Id) ?? [];
                var latestDelivery = deliveryLogs.FirstOrDefault();
                var concludedStatus = reviews.FirstOrDefault()?.Status switch
                {
                    ParticipantReview.ParticipantReviewStatus.Accepted =>
                        ParticipantConcludedStatus.Accepted,
                    ParticipantReview.ParticipantReviewStatus.Rejected =>
                        ParticipantConcludedStatus.Rejected,
                    _ => ParticipantConcludedStatus.Pending,
                };

                return new ParticipantItem
                {
                    CreatedAt = p.JoinedAt,
                    WithdrawnAt = p.WithdrawnAt,
                    IsWithdrawn = p.WithdrawnAt is not null,
                    IsBanned = userInfo?.BannedAt is not null,
                    BannedAt = userInfo?.BannedAt,
                    BanReason = userInfo?.BanReason,
                    Id = p.UserId,
                    Name = userInfo?.Name ?? "Unknown",
                    Email = userInfo?.Email,
                    TeamId = p.TeamId,
                    TeamName = p.TeamId.HasValue ? teams.GetValueOrDefault(p.TeamId.Value) : null,
                    ConcludedStatus = concludedStatus,
                    Reviews =
                    [
                        .. reviews.Select(r => new ParticipantReviewItem
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
                    RegistrationSubmissions =
                        submissionsByParticipant.GetValueOrDefault(p.Id) ?? [],
                    EmailSentCount = deliveryLogs.Count(x =>
                        x.Status == ParticipantEmailDelivery.EmailDeliveryStatus.Sent
                    ),
                    LastEmailSentAt = latestDelivery?.SentAt,
                    LastEmailStatus = latestDelivery?.Status.ToString(),
                };
            })
            .ToList();

        var activeParticipants = participantResponses.Where(p => !p.IsWithdrawn).ToList();

        await Send.OkAsync(
            new Response
            {
                Participants = participantResponses,
                TotalCount = participants.Count,
                PendingCount = activeParticipants.Count(p =>
                    p.ConcludedStatus == ParticipantConcludedStatus.Pending
                ),
                AcceptedCount = activeParticipants.Count(p =>
                    p.ConcludedStatus == ParticipantConcludedStatus.Accepted
                ),
                RejectedCount = activeParticipants.Count(p =>
                    p.ConcludedStatus == ParticipantConcludedStatus.Rejected
                ),
            },
            ct
        );
    }
}
