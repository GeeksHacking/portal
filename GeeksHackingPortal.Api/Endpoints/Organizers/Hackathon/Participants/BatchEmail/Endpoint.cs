using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Constants;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Participants.BatchEmail;

public class Endpoint(
    ISqlSugarClient sql,
    IEmailService emailService,
    INotificationTemplateResolver notificationTemplateResolver
) : Endpoint<Request, Response>
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
        var statusFilter = req.Status.ToLowerInvariant();
        if (statusFilter is not ("all" or "accepted" or "rejected"))
        {
            AddError("Status must be 'All', 'Accepted', or 'Rejected'");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var emailTemplates = await notificationTemplateResolver.GetHackathonTemplatesAsync(
            hackathon.Id,
            ct
        );

        // Get all participants for the hackathon
        var participantsQuery = sql.Queryable<Participant>()
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
        var users = await sql.Queryable<User>().Where(u => userIds.Contains(u.Id)).ToListAsync(ct);
        var userDict = users.ToDictionary(u => u.Id);

        var reviews = await sql.Queryable<ParticipantReview>()
            .Where(r => participantIds.Contains(r.ParticipantId))
            .OrderBy(r => r.CreatedAt, OrderByType.Desc)
            .OrderBy(r => r.Id, OrderByType.Desc)
            .ToListAsync(ct);

        var latestReviewsByParticipant = reviews
            .GroupBy(r => r.ParticipantId)
            .ToDictionary(g => g.Key, g => g.First());

        // Build list of participants to email based on status filter
        var emailDispatches =
            new List<(
                Participant Participant,
                User User,
                string EventKey,
                string TemplateId,
                TemplatedEmailRequest EmailRequest
            )>();
        var emailDeliveryLogs = new List<ParticipantEmailDelivery>();
        var errors = new List<string>();

        var acceptedCount = 0;
        var rejectedCount = 0;

        foreach (var participant in participants)
        {
            if (!userDict.TryGetValue(participant.UserId, out var user))
            {
                errors.Add($"Participant {participant.Id} has no associated user record.");
                continue;
            }

            if (!latestReviewsByParticipant.TryGetValue(participant.Id, out var review))
            {
                continue;
            }

            var (reviewStatus, eventKey) = review.Status switch
            {
                ParticipantReview.ParticipantReviewStatus.Accepted => (
                    "Accepted",
                    NotificationEventKeys.ParticipantReviewAccepted
                ),
                ParticipantReview.ParticipantReviewStatus.Rejected => (
                    "Rejected",
                    NotificationEventKeys.ParticipantReviewRejected
                ),
                _ => (null, null),
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

            if (
                string.IsNullOrWhiteSpace(eventKey)
                || !emailTemplates.TryGetValue(eventKey, out var templateId)
                || string.IsNullOrWhiteSpace(templateId)
            )
            {
                errors.Add(
                    $"No email template configured for event '{eventKey ?? "unknown"}' in hackathon {hackathon.Id}."
                );
                if (!string.IsNullOrWhiteSpace(eventKey))
                {
                    emailDeliveryLogs.Add(
                        new ParticipantEmailDelivery
                        {
                            Id = Guid.NewGuid(),
                            HackathonId = hackathon.Id,
                            ParticipantId = participant.Id,
                            UserId = user.Id,
                            ToEmail = user.Email,
                            EventKey = eventKey,
                            TemplateId = string.Empty,
                            Provider = "postmark",
                            Status = ParticipantEmailDelivery.EmailDeliveryStatus.Skipped,
                            ErrorMessage = $"No template configured for event '{eventKey}'",
                            SentAt = DateTimeOffset.UtcNow,
                        }
                    );
                }
                continue;
            }

            var templateVariables = ParticipantReviewEmailTemplateModelFactory.Create(
                participant,
                user,
                hackathon,
                reviewStatus,
                review.Reason
            );
            emailDispatches.Add(
                (
                    participant,
                    user,
                    eventKey,
                    templateId,
                    new TemplatedEmailRequest(
                        user.Email,
                        templateId,
                        templateVariables,
                        Guid.NewGuid().ToString("N")
                    )
                )
            );

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
        if (emailDispatches.Count > 0)
        {
            var results = await emailService.SendBatchTemplatedEmailsAsync(
                emailDispatches.Select(x => x.EmailRequest),
                ct
            );
            var resultByCorrelationId = results
                .Where(r => !string.IsNullOrWhiteSpace(r.CorrelationId))
                .ToDictionary(r => r.CorrelationId!, r => r);

            foreach (var dispatch in emailDispatches)
            {
                TemplatedEmailSendResult sendResult;
                if (
                    !string.IsNullOrWhiteSpace(dispatch.EmailRequest.CorrelationId)
                    && resultByCorrelationId.TryGetValue(
                        dispatch.EmailRequest.CorrelationId!,
                        out var matchedResult
                    )
                )
                {
                    sendResult = matchedResult;
                }
                else
                {
                    sendResult = new TemplatedEmailSendResult(
                        dispatch.User.Email,
                        dispatch.TemplateId,
                        "postmark",
                        TemplatedEmailSendResult.SendStatus.Failed,
                        DateTimeOffset.UtcNow,
                        ErrorMessage: "No send result returned from email service",
                        CorrelationId: dispatch.EmailRequest.CorrelationId
                    );
                }

                emailDeliveryLogs.Add(
                    new ParticipantEmailDelivery
                    {
                        Id = Guid.NewGuid(),
                        HackathonId = hackathon.Id,
                        ParticipantId = dispatch.Participant.Id,
                        UserId = dispatch.User.Id,
                        ToEmail = dispatch.User.Email,
                        EventKey = dispatch.EventKey,
                        TemplateId = dispatch.TemplateId,
                        Provider = sendResult.Provider,
                        ProviderMessageId = sendResult.ProviderMessageId,
                        Status = sendResult.Status switch
                        {
                            TemplatedEmailSendResult.SendStatus.Sent => ParticipantEmailDelivery
                                .EmailDeliveryStatus
                                .Sent,
                            TemplatedEmailSendResult.SendStatus.Failed => ParticipantEmailDelivery
                                .EmailDeliveryStatus
                                .Failed,
                            _ => ParticipantEmailDelivery.EmailDeliveryStatus.Skipped,
                        },
                        ErrorMessage = sendResult.ErrorMessage,
                        SentAt = sendResult.SentAt,
                    }
                );
            }
        }

        if (emailDeliveryLogs.Count > 0)
        {
            await sql.Insertable(emailDeliveryLogs).ExecuteCommandAsync(ct);
        }

        await Send.OkAsync(
            new Response
            {
                TotalEmailsSent = emailDispatches.Count,
                AcceptedEmailsSent = acceptedCount,
                RejectedEmailsSent = rejectedCount,
                Errors = errors,
            },
            ct
        );
    }
}
