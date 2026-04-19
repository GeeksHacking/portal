using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Participants.Withdraw;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId:guid}/participants/{UserId:guid}/withdraw");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Participants").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Withdraw a participant";
            s.Description =
                "Withdraws a participant from the hackathon. Organizers can withdraw any active participant.";
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

        if (hackathon.EventEndDate < DateTimeOffset.UtcNow)
        {
            AddError("You cannot withdraw a participant after the event has ended");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var participant = await sql.Queryable<Participant>()
            .Where(p =>
                p.HackathonId == hackathon.Id
                && p.UserId == req.UserId
                && p.WithdrawnAt == null
            )
            .FirstAsync(ct);

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        participant.TeamId = null;
        participant.WithdrawnAt = DateTimeOffset.UtcNow;
        await sql.Updateable(participant).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response { Message = "Participant has been withdrawn from the hackathon" },
            ct
        );
    }
}
