using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.List;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/participants/{UserId:guid}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Participants"));
        Summary(s =>
        {
            s.Summary = "Get participant details";
            s.Description =
                "Retrieves full details for a participant, including registration responses.";
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
            .Includes(p => p.ParticipantReviews)
            .Includes(p => p.Team)
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == req.UserId)
            .FirstAsync(ct);

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var user = await sql.Queryable<User>().InSingleAsync(participant.UserId);
        var participantReviews = participant.ParticipantReviews;
        var emailDeliveries = await sql.Queryable<ParticipantEmailDelivery>()
            .Where(e => e.ParticipantId == participant.Id)
            .OrderByDescending(e => e.SentAt)
            .ToListAsync(ct);
        var userName = user is null ? "Unknown" : $"{user.FirstName} {user.LastName}";

        var concludedStatus = participant.ConcludedStatus switch
        {
            ParticipantReview.ParticipantReviewStatus.Accepted =>
                ParticipantConcludedStatus.Accepted,
            ParticipantReview.ParticipantReviewStatus.Rejected =>
                ParticipantConcludedStatus.Rejected,
            _ => ParticipantConcludedStatus.Pending,
        };

        await Send.OkAsync(
            new Response
            {
                CreatedAt = participant.JoinedAt,
                Id = participant.UserId,
                Name = userName,
                Email = user?.Email,
                TeamId = participant.TeamId,
                TeamName = participant.Team?.Name,
                ConcludedStatus = concludedStatus,
                Reviews =
                [
                    .. participantReviews.Select(r => new ParticipantReviewItem
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
                RegistrationSubmissions = await sql.Queryable<ParticipantRegistrationSubmission>()
                    .LeftJoin<RegistrationQuestion>((s, q) => s.QuestionId == q.Id)
                    .Where((s, q) => s.ParticipantId == participant.Id)
                    .Select(
                        (s, q) =>
                            new RegistrationSubmissionItem
                            {
                                QuestionId = s.QuestionId,
                                QuestionText = q.QuestionText,
                                Value = s.Value,
                                FollowUpValue = s.FollowUpValue,
                                UpdatedAt = s.UpdatedAt,
                            }
                    )
                    .ToListAsync(ct),
                EmailSentCount = emailDeliveries.Count(e =>
                    e.Status == ParticipantEmailDelivery.EmailDeliveryStatus.Sent
                ),
                LastEmailSentAt = emailDeliveries.FirstOrDefault()?.SentAt,
                LastEmailStatus = emailDeliveries.FirstOrDefault()?.Status.ToString(),
                EmailDeliveries =
                [
                    .. emailDeliveries.Select(e => new ParticipantEmailDeliveryItem
                    {
                        Id = e.Id,
                        EventKey = e.EventKey,
                        TemplateId = e.TemplateId,
                        Provider = e.Provider,
                        Status = e.Status.ToString(),
                        ErrorMessage = e.ErrorMessage,
                        ProviderMessageId = e.ProviderMessageId,
                        SentAt = e.SentAt,
                    }),
                ],
            },
            ct
        );
    }
}
