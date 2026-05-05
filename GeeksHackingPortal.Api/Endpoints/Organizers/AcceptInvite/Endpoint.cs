using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.AcceptInvite;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/accept-invite");
        Description(b =>
            b.WithTags("Activity Organizers")
                .WithDescription(
                    "Accepts an organizer invite code and adds the current user as an organizer for the associated activity."
                )
        );
        Summary(s =>
        {
            s.Summary = "Accept an organizer invite";
            s.Description =
                "Redeems an invite code and registers the current user as an organizer for the activity the code was issued for.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var invite = await sql.Queryable<ActivityOrganizerInvite>()
            .Where(i => i.Code == req.Code)
            .FirstAsync(ct);

        if (invite is null)
        {
            AddError(r => r.Code, "Invite code not found.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (invite.UsedAt is not null)
        {
            AddError(r => r.Code, "Invite code has already been used.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (invite.ExpiresAt is not null && invite.ExpiresAt < DateTimeOffset.UtcNow)
        {
            AddError(r => r.Code, "Invite code has expired.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var activity = await sql.Queryable<Activity>().InSingleAsync(invite.ActivityId);
        if (activity is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var alreadyOrganizer = await sql.Queryable<ActivityOrganizer>()
            .AnyAsync(o => o.ActivityId == invite.ActivityId && o.UserId == userId.Value, ct);
        if (alreadyOrganizer)
        {
            AddError(r => r.Code, "You are already an organizer for this activity.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        invite.UsedByUserId = userId.Value;
        invite.UsedAt = now;

        var activityOrganizerId = Guid.NewGuid();
        var activityOrganizer = new ActivityOrganizer
        {
            Id = activityOrganizerId,
            ActivityId = invite.ActivityId,
            UserId = userId.Value,
            Type = invite.Type,
            CreatedAt = now,
        };

        if (activity.Kind == ActivityKind.Hackathon)
        {
            var organizer = new Organizer
            {
                Id = activityOrganizerId,
                HackathonId = invite.ActivityId,
                UserId = userId.Value,
                Type = invite.Type,
            };

            var transactionResult = await sql.Ado.UseTranAsync(async () =>
            {
                await sql.Insertable(organizer).ExecuteCommandAsync(ct);
                await sql.Insertable(activityOrganizer).ExecuteCommandAsync(ct);
                await sql.Updateable(invite)
                    .UpdateColumns(i => new { i.UsedByUserId, i.UsedAt })
                    .ExecuteCommandAsync(ct);
            });

            if (!transactionResult.IsSuccess)
            {
                throw transactionResult.ErrorException!;
            }
        }
        else
        {
            var transactionResult = await sql.Ado.UseTranAsync(async () =>
            {
                await sql.Insertable(activityOrganizer).ExecuteCommandAsync(ct);
                await sql.Updateable(invite)
                    .UpdateColumns(i => new { i.UsedByUserId, i.UsedAt })
                    .ExecuteCommandAsync(ct);
            });

            if (!transactionResult.IsSuccess)
            {
                throw transactionResult.ErrorException!;
            }
        }

        await Send.OkAsync(
            new Response { ActivityId = invite.ActivityId, Type = invite.Type },
            ct
        );
    }
}
