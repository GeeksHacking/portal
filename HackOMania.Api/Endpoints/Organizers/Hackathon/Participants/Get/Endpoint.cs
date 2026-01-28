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

        var participantData = await sql.Queryable<Participant>()
            .Includes(p => p.ParticipantReviews)
            .LeftJoin<User>((p, u) => p.UserId == u.Id)
            .LeftJoin<Team>((p, u, t) => p.TeamId == t.Id)
            .Where((p, u, t) => p.HackathonId == hackathon.Id && p.UserId == req.UserId)
            .Select(
                (p, u, t) =>
                    new ParticipantDetails
                    {
                        Participant = p,
                        UserName = u.FirstName + " " + u.LastName,
                        TeamName = t.Name,
                    }
            )
            .FirstAsync(ct);

        if (participantData is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var participant = participantData.Participant;
        var participantReviews = await sql.Queryable<ParticipantReview>()
            .Where(r => r.ParticipantId == participant.Id)
            .ToListAsync(ct);

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
                Id = participant.UserId,
                Name = participantData.UserName ?? "Unknown",
                TeamId = participant.TeamId,
                TeamName = participantData.TeamName,
                ConcludedStatus = concludedStatus,
                Reviews = participant
                    .ParticipantReviews.Select(r => new ParticipantReviewItem
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
                    })
                    .ToList(),
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
                            }
                    )
                    .ToListAsync(ct),
            },
            ct
        );
    }

    private sealed record ParticipantDetails
    {
        public required Participant Participant { get; init; }
        public string? UserName { get; init; }
        public string? TeamName { get; init; }
    }
}
