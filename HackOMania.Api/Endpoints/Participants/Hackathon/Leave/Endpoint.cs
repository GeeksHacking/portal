using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Leave;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/leave");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Hackathons").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Leave a hackathon";
            s.Description =
                "Removes the current user from the hackathon. The participant must not be in a team before leaving. Historical records are preserved.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .WithCache()
            .InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var currentUserId = User.GetUserId();

        var participant = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == currentUserId && p.LeftAt == null)
            .WithCache()
            .FirstAsync(ct);

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (participant.TeamId is not null)
        {
            AddError("You must leave your team before leaving the hackathon");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        participant.LeftAt = DateTimeOffset.UtcNow;
        await sql.Updateable(participant).ExecuteCommandAsync(ct);

        await Send.OkAsync(new Response { Message = "You have left the hackathon" }, ct);
    }
}
