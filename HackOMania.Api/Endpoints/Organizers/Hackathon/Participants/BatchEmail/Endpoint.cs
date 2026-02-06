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
            string Status,
            Dictionary<string, object> TemplateVariables
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

            // Build comprehensive template variables with all available context
            var templateVariables = new Dictionary<string, object>
            {
                // User/Participant information
                ["participant_name"] = user.Name,
                ["participant_first_name"] = user.FirstName,
                ["participant_last_name"] = user.LastName,
                ["participant_email"] = user.Email,
                ["participant_id"] = participant.Id.ToString(),
                ["user_id"] = user.Id.ToString(),
                
                // Hackathon information
                ["hackathon_name"] = hackathon.Name,
                ["hackathon_id"] = hackathon.Id.ToString(),
                ["hackathon_short_code"] = hackathon.ShortCode,
                ["hackathon_venue"] = hackathon.Venue,
                ["hackathon_description"] = hackathon.Description,
                ["hackathon_homepage_url"] = hackathon.HomepageUri.ToString(),
                
                // Event dates
                ["event_start_date"] = hackathon.EventStartDate.ToString("yyyy-MM-dd"),
                ["event_end_date"] = hackathon.EventEndDate.ToString("yyyy-MM-dd"),
                ["event_start_date_formatted"] = hackathon.EventStartDate.ToString("MMMM dd, yyyy"),
                ["event_end_date_formatted"] = hackathon.EventEndDate.ToString("MMMM dd, yyyy"),
                
                // Submissions dates
                ["submissions_start_date"] = hackathon.SubmissionsStartDate.ToString("yyyy-MM-dd"),
                ["submissions_end_date"] = hackathon.SubmissionsEndDate.ToString("yyyy-MM-dd"),
                ["submissions_start_date_formatted"] = hackathon.SubmissionsStartDate.ToString("MMMM dd, yyyy"),
                ["submissions_end_date_formatted"] = hackathon.SubmissionsEndDate.ToString("MMMM dd, yyyy"),
                
                // Review information
                ["reason"] = review.Reason ?? string.Empty,
                ["has_reason"] = !string.IsNullOrWhiteSpace(review.Reason),
                ["review_status"] = reviewStatus,
                
                // Participant metadata
                ["joined_at"] = participant.JoinedAt.ToString("yyyy-MM-dd"),
                ["joined_at_formatted"] = participant.JoinedAt.ToString("MMMM dd, yyyy"),
            };

            participantsToEmail.Add((user.Email, reviewStatus, templateVariables));

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
