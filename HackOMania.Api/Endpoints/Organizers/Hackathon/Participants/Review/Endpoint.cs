using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.Review;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{Id}/participants/{ParticipantUserId}/review");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Participants"));
        Summary(s =>
        {
            s.Summary = "Review a participant (Organizer)";
            s.Description =
                "Accept or reject a participant's application. Requires organizer access.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h => h.Id.ToString() == req.Id || h.ShortCode == req.Id)
            .FirstAsync(ct);

        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var participant = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == req.ParticipantUserId)
            .FirstAsync(ct);

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
            ParticipantId = participant.UserId,
            Status = status,
            Reason = req.Reason,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await sql.Insertable(review).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                ParticipantUserId = participant.UserId,
                Status = status.ToString(),
                ReviewedAt = review.CreatedAt,
            },
            ct
        );
    }
}
