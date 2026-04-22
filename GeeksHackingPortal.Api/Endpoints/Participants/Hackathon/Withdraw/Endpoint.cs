using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Withdraw;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/withdraw");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Hackathons").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Withdraw from a hackathon";
            s.Description =
                "Withdraws the current user from the hackathon. The participant must not be in a team before withdrawing. Historical records are preserved.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Includes(h => h.Activity)
            .WithCache()
            .InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (hackathon.EventEndDate < DateTimeOffset.UtcNow)
        {
            AddError("You cannot leave a hackathon after the event has ended");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var currentUserId = User.GetUserId();

        var participant = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == currentUserId && p.WithdrawnAt == null)
            .WithCache()
            .FirstAsync(ct);

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (participant.TeamId is not null)
        {
            AddError("You must leave your team before withdrawing from the hackathon");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var withdrawnAt = DateTimeOffset.UtcNow;
        participant.WithdrawnAt = withdrawnAt;

        var registration = await sql.Queryable<ActivityRegistration>()
            .InSingleAsync(participant.Id);

        if (registration is not null)
        {
            registration.Status = ActivityRegistrationStatus.Withdrawn;
            registration.WithdrawnAt = withdrawnAt;
        }

        var transactionResult = await sql.Ado.UseTranAsync(async () =>
        {
            await sql.Updateable(participant).ExecuteCommandAsync(ct);
            if (registration is not null)
            {
                await sql.Updateable(registration).ExecuteCommandAsync(ct);
            }
        });

        if (!transactionResult.IsSuccess)
        {
            throw transactionResult.ErrorException!;
        }

        await Send.OkAsync(new Response { Message = "You have withdrawn from the hackathon" }, ct);
    }
}
