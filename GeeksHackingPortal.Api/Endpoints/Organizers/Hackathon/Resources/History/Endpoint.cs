using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.History;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get(
            "organizers/hackathons/{HackathonId:guid}/participants/{ParticipantUserId:guid}/resources/{ResourceId:guid}/history"
        );
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Resources"));
        Summary(s =>
        {
            s.Summary = "Get participant resource redemption history";
            s.Description = "Returns a participant's redemption history for a specific resource.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var participantData = await sql.Queryable<Participant>()
            .LeftJoin<User>((p, u) => p.UserId == u.Id)
            .Where((p, u) => p.UserId == req.ParticipantUserId && p.HackathonId == req.HackathonId)
            .Select((p, u) => new { Participant = p, User = u })
            .FirstAsync(ct);

        if (participantData is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resource = await sql.Queryable<Resource>()
            .Where(r => r.Id == req.ResourceId && r.HackathonId == req.HackathonId)
            .FirstAsync(ct);

        if (resource is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var history = await sql.Queryable<ResourceRedemption>()
            .Where(r =>
                r.HackathonId == req.HackathonId
                && r.ResourceId == req.ResourceId
                && r.RedeemerId == req.ParticipantUserId
            )
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        await Send.OkAsync(
            new Response
            {
                ParticipantId = participantData.Participant.Id,
                UserId = participantData.Participant.UserId,
                UserName = $"{participantData.User.FirstName} {participantData.User.LastName}".Trim(),
                ResourceId = resource.Id,
                ResourceName = resource.Name,
                ResourceIsPublished = resource.IsPublished,
                HasRedeemed = history.Count > 0,
                RedemptionCount = history.Count,
                History =
                [
                    .. history.Select(r => new HistoryItemDto
                    {
                        RedemptionId = r.Id,
                        CreatedAt = r.CreatedAt.AssumeStoredAsUtc(),
                    }),
                ],
            },
            ct
        );
    }
}
