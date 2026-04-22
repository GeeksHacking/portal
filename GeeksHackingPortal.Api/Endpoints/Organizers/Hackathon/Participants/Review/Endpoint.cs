using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Constants;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Participants.Review;

public class Endpoint(
    ISqlSugarClient sql,
    IEmailService emailService,
    INotificationTemplateResolver notificationTemplateResolver
) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId:guid}/participants/{ParticipantUserId}/review");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Participants"));
        Summary(s =>
        {
            s.Summary = "Review a participant";
            s.Description = "Accept or reject a participant's application.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var decision = req.Decision.ToLowerInvariant();
        if (decision is not ("accept" or "reject"))
        {
            AddError("Decision must be 'accept' or 'reject'");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }
        var normalizedReason = req.Reason?.Trim() ?? string.Empty;
        if (decision == "reject" && string.IsNullOrWhiteSpace(normalizedReason))
        {
            AddError("A reason is required when rejecting a participant.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var hackathon = await sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity).InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var status =
            decision == "accept"
                ? ParticipantReview.ParticipantReviewStatus.Accepted
                : ParticipantReview.ParticipantReviewStatus.Rejected;

        Participant? participant = null;
        ParticipantReview? review = null;

        var reviewedAt = DateTimeOffset.UtcNow;
        var transactionResult = await sql.Ado.UseTranAsync(async () =>
        {
            participant = await sql.Queryable<Participant>()
                .Where(p => p.HackathonId == hackathon.Id && p.UserId == req.ParticipantUserId)
                .TranLock(DbLockType.Wait)
                .SingleAsync();

            if (participant is null)
            {
                return;
            }

            review = new ParticipantReview
            {
                Id = Guid.NewGuid(),
                ParticipantId = participant.Id,
                Status = status,
                Reason = normalizedReason,
                CreatedAt = reviewedAt,
            };

            await sql.Insertable(review).ExecuteCommandAsync(ct);
        });

        if (!transactionResult.IsSuccess)
        {
            throw transactionResult.ErrorException!;
        }

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (review is null)
        {
            ThrowError("Unable to create review.");
            return;
        }

        // Send participant review email notification without blocking the review flow.
        var user = await sql.Queryable<User>().InSingleAsync(participant.UserId);
        if (user is not null)
        {
            var eventKey = status switch
            {
                ParticipantReview.ParticipantReviewStatus.Accepted =>
                    NotificationEventKeys.ParticipantReviewAccepted,
                ParticipantReview.ParticipantReviewStatus.Rejected =>
                    NotificationEventKeys.ParticipantReviewRejected,
                _ => throw new ArgumentOutOfRangeException(),
            };

            if (!string.IsNullOrWhiteSpace(eventKey))
            {
                var templateId = await notificationTemplateResolver.ResolveTemplateIdAsync(
                    hackathon.Id,
                    eventKey,
                    ct
                );

                if (string.IsNullOrWhiteSpace(templateId))
                {
                    await sql.Insertable(
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
                        )
                        .ExecuteCommandAsync(ct);
                }
                else
                {
                    var reviewStatus = status.ToString();
                    var templateVariables = ParticipantReviewEmailTemplateModelFactory.Create(
                        participant,
                        user,
                        hackathon,
                        reviewStatus,
                        normalizedReason
                    );

                    var sendResult = await emailService.SendTemplatedEmailAsync(
                        new TemplatedEmailRequest(
                            user.Email,
                            templateId,
                            templateVariables,
                            Guid.NewGuid().ToString("N")
                        ),
                        ct
                    );

                    await sql.Insertable(
                            new ParticipantEmailDelivery
                            {
                                Id = Guid.NewGuid(),
                                HackathonId = hackathon.Id,
                                ParticipantId = participant.Id,
                                UserId = user.Id,
                                ToEmail = user.Email,
                                EventKey = eventKey,
                                TemplateId = templateId,
                                Provider = sendResult.Provider,
                                ProviderMessageId = sendResult.ProviderMessageId,
                                Status = sendResult.Status switch
                                {
                                    TemplatedEmailSendResult.SendStatus.Sent =>
                                        ParticipantEmailDelivery.EmailDeliveryStatus.Sent,
                                    TemplatedEmailSendResult.SendStatus.Failed =>
                                        ParticipantEmailDelivery.EmailDeliveryStatus.Failed,
                                    _ => ParticipantEmailDelivery.EmailDeliveryStatus.Skipped,
                                },
                                ErrorMessage = sendResult.ErrorMessage,
                                SentAt = sendResult.SentAt,
                            }
                        )
                        .ExecuteCommandAsync(ct);
                }
            }
        }

        await Send.OkAsync(
            new Response
            {
                ParticipantId = participant.Id,
                Status = status.ToString(),
                ReviewedAt = review.CreatedAt,
            },
            ct
        );
    }
}
