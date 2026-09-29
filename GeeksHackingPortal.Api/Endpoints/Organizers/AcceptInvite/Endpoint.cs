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
                "Redeems an invite code and registers the current user as an organizer for the activity the code was issued for. Codes are reusable unless a MaxUses limit is set.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var code = req.Code?.Trim().ToUpperInvariant();
        var invite = await sql.Queryable<ActivityOrganizerInvite>()
            .Where(i => i.Code == code)
            .FirstAsync(ct);

        if (invite is null)
        {
            AddError(r => r.Code, "Invite code not found.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (invite.ExpiresAt is not null && invite.ExpiresAt < DateTimeOffset.UtcNow)
        {
            AddError(r => r.Code, "Invite code has expired.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Serialize redemptions of this invite so concurrent requests cannot all pass the
        // MaxUses check before any of them records a use. Disposing without commit rolls back.
        using var tran = sql.Ado.UseTran();
        await sql.LockRowAsync<ActivityOrganizerInvite>(invite.Id);

        if (invite.MaxUses is not null)
        {
            var useCount = await sql.Queryable<ActivityOrganizerInviteUse>()
                .CountAsync(u => u.InviteId == invite.Id, ct);
            if (useCount >= invite.MaxUses)
            {
                AddError(r => r.Code, "Invite code has reached its maximum number of uses.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }
        }

        var alreadyUsed = await sql.Queryable<ActivityOrganizerInviteUse>()
            .AnyAsync(u => u.InviteId == invite.Id && u.UserId == userId.Value, ct);
        if (alreadyUsed)
        {
            await Send.OkAsync(
                new Response { ActivityId = invite.ActivityId, Type = invite.Type },
                ct
            );
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
            await Send.OkAsync(
                new Response { ActivityId = invite.ActivityId, Type = invite.Type },
                ct
            );
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var activityOrganizerId = Guid.NewGuid();
        var activityOrganizer = new ActivityOrganizer
        {
            Id = activityOrganizerId,
            ActivityId = invite.ActivityId,
            UserId = userId.Value,
            Type = invite.Type,
            CreatedAt = now,
        };
        var inviteUse = new ActivityOrganizerInviteUse
        {
            Id = Guid.NewGuid(),
            InviteId = invite.Id,
            UserId = userId.Value,
            UsedAt = now,
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
            await sql.Insertable(organizer).ExecuteCommandAsync(ct);
        }

        await sql.Insertable(activityOrganizer).ExecuteCommandAsync(ct);
        await sql.Insertable(inviteUse).ExecuteCommandAsync(ct);

        tran.CommitTran();

        await Send.OkAsync(
            new Response { ActivityId = invite.ActivityId, Type = invite.Type },
            ct
        );
    }
}

