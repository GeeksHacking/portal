using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Organizers.Add;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId:guid}/organizers");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b =>
            b.WithTags("Activity Organizers")
                .WithDescription(
                    "It might be better to shift this to using an invite code instead, since having organizers know each others user ID prior to adding is not very user friendly."
                )
        );
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity).InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var targetUser = await sql.Queryable<User>().InSingleAsync(req.UserId);
        if (targetUser is null)
        {
            AddError(r => r.UserId, "User not found.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var exists = await sql.Queryable<Organizer>()
            .AnyAsync(o => o.UserId == req.UserId && o.HackathonId == hackathon.Id, ct);

        if (exists)
        {
            AddError(r => r.UserId, "User is already an organizer for this hackathon.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var organizer = new Organizer
        {
            Id = Guid.NewGuid(),
            UserId = req.UserId,
            HackathonId = hackathon.Id,
            Type = req.Type,
        };
        var activityOrganizer = new ActivityOrganizer
        {
            Id = organizer.Id,
            ActivityId = hackathon.Id,
            UserId = req.UserId,
            Type = req.Type,
        };

        var transactionResult = await sql.Ado.UseTranAsync(async () =>
        {
            await sql.Insertable(organizer).ExecuteCommandAsync(ct);
            await sql.Insertable(activityOrganizer).ExecuteCommandAsync(ct);
        });

        if (!transactionResult.IsSuccess)
        {
            throw transactionResult.ErrorException!;
        }

        await Send.OkAsync(new Response { UserId = req.UserId, Type = req.Type }, ct);
    }
}
