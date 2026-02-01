using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.BatchEmail;

public class Endpoint(ISqlSugarClient sql, IEmailService emailService)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId:guid}/participants/batch-email");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Participants"));
        Summary(s =>
        {
            s.Summary = "Send batch emails to participants";
            s.Description =
                "Send acceptance or rejection emails to multiple participants based on their review status.";
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

        // Get all participants for the hackathon
        var participantsQuery = sql
            .Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id);

        // Filter by specific participant IDs if provided
        if (req.ParticipantUserIds is not null && req.ParticipantUserIds.Count > 0)
        {
            participantsQuery = participantsQuery.Where(p =>
                req.ParticipantUserIds.Contains(p.UserId)
            );
        }

        var participants = await participantsQuery.ToListAsync(ct);

        if (participants.Count == 0)
        {
            await Send.OkAsync(
                new Response
                {
                    TotalEmailsSent = 0,
                    AcceptedEmailsSent = 0,
                    RejectedEmailsSent = 0,
                },
                ct
            );
            return;
        }

        var participantIds = participants.Select(p => p.Id).ToList();
        var userIds = participants.Select(p => p.UserId).ToList();

        // Get users
        var users = await sql
            .Queryable<User>()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);
        var userDict = users.ToDictionary(u => u.Id);

        // Get latest reviews for each participant using a more efficient approach
        // Group by ParticipantId and get the max CreatedAt, then fetch those specific reviews
        var latestReviewDates = await sql
            .Queryable<ParticipantReview>()
            .Where(r => participantIds.Contains(r.ParticipantId))
            .GroupBy(r => r.ParticipantId)
            .Select(g => new { ParticipantId = g.ParticipantId, MaxDate = SqlFunc.AggregateMax(g.CreatedAt) })
            .ToListAsync(ct);

        var latestReviewsByParticipant = new Dictionary<Guid, ParticipantReview>();

        if (latestReviewDates.Count > 0)
        {
            // Fetch only the latest reviews
            var reviews = await sql
                .Queryable<ParticipantReview>()
                .Where(r => participantIds.Contains(r.ParticipantId))
                .ToListAsync(ct);

            // Build dictionary with latest review per participant
            foreach (var reviewDate in latestReviewDates)
            {
                var latestReview = reviews
                    .Where(r => r.ParticipantId == reviewDate.ParticipantId && r.CreatedAt == reviewDate.MaxDate)
                    .FirstOrDefault();
                if (latestReview != null)
                {
                    latestReviewsByParticipant[reviewDate.ParticipantId] = latestReview;
                }
            }
        }

        // Build list of participants to email based on status filter
        var participantsToEmail = new List<(
            string Email,
            string Name,
            string Status,
            string? Reason
        )>();

        var statusFilter = req.Status.ToLowerInvariant();
        var acceptedCount = 0;
        var rejectedCount = 0;

        foreach (var participant in participants)
        {
            if (!userDict.TryGetValue(participant.UserId, out var user))
            {
                continue;
            }

            if (!latestReviewsByParticipant.TryGetValue(participant.Id, out var review))
            {
                // Skip participants without reviews
                continue;
            }

            var reviewStatus = review.Status switch
            {
                ParticipantReview.ParticipantReviewStatus.Accepted => "Accepted",
                ParticipantReview.ParticipantReviewStatus.Rejected => "Rejected",
                _ => null,
            };

            if (reviewStatus is null)
            {
                continue;
            }

            // Apply status filter
            if (
                statusFilter != "all"
                && !reviewStatus.Equals(statusFilter, StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            participantsToEmail.Add((user.Email, user.Name, reviewStatus, review.Reason));

            if (reviewStatus == "Accepted")
            {
                acceptedCount++;
            }
            else if (reviewStatus == "Rejected")
            {
                rejectedCount++;
            }
        }

        // Send batch emails
        if (participantsToEmail.Count > 0)
        {
            await emailService.SendBatchEmailsAsync(
                participantsToEmail,
                hackathon.Name,
                hackathon.AcceptedEmailTemplateId,
                hackathon.RejectedEmailTemplateId,
                ct
            );
        }

        await Send.OkAsync(
            new Response
            {
                TotalEmailsSent = participantsToEmail.Count,
                AcceptedEmailsSent = acceptedCount,
                RejectedEmailsSent = rejectedCount,
            },
            ct
        );
    }
}
