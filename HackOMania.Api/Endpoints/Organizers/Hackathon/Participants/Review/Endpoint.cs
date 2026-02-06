using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Constants;
using HackOMania.Api.Entities;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.Review;

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
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var participant = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == req.ParticipantUserId)
            .SingleAsync();

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var decision = req.Decision.ToLowerInvariant();
        if (decision is not ("accept" or "reject"))
        {
            AddError("Decision must be 'accept' or 'reject'");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var status =
            decision == "accept"
                ? ParticipantReview.ParticipantReviewStatus.Accepted
                : ParticipantReview.ParticipantReviewStatus.Rejected;

        var review = new ParticipantReview
        {
            Id = Guid.NewGuid(),
            ParticipantId = participant.Id,
            Status = status,
            Reason = req.Reason ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await sql.Insertable(review).ExecuteCommandAsync(ct);

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
                _ => null,
            };

            if (!string.IsNullOrWhiteSpace(eventKey))
            {
                var templateId = await notificationTemplateResolver.ResolveTemplateIdAsync(
                    hackathon.Id,
                    eventKey,
                    ct
                );

                if (!string.IsNullOrWhiteSpace(templateId))
                {
                    var reviewStatus = status.ToString();
                    var templateVariables = ParticipantReviewEmailTemplateModelFactory.Create(
                        participant,
                        user,
                        hackathon,
                        reviewStatus,
                        req.Reason
                    );

                    await emailService.SendTemplatedEmailAsync(
                        new TemplatedEmailRequest(user.Email, templateId, templateVariables),
                        ct
                    );
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
