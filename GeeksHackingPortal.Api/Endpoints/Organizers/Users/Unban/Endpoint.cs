using FastEndpoints;
using GeeksHackingPortal.Api.Endpoints.Organizers.Users.Shared;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Users.Unban;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, UserBanStateResponse>
{
    public override void Configure()
    {
        Delete("organizers/users/{UserId:guid}/ban");
        Description(b => b.WithTags("Organizers", "Users"));
        Summary(s =>
        {
            s.Summary = "Unban a user account";
            s.Description = "Allows any organizer or root admin to remove a global user ban.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var actorUserId = User.GetUserId();
        if (actorUserId is null || !await membership.IsAnyOrganizerOrRoot(actorUserId.Value, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var targetUser = await sql.Queryable<Entities.User>().InSingleAsync(req.UserId);
        if (targetUser is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        targetUser.BannedAt = null;
        targetUser.BanReason = null;
        targetUser.BannedByUserId = null;

        await sql.Updateable(targetUser).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new UserBanStateResponse
            {
                UserId = targetUser.Id,
                IsBanned = false,
                BannedAt = null,
                BanReason = null,
            },
            ct
        );
    }
}
