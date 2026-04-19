using FastEndpoints;
using GeeksHackingPortal.Api.Endpoints.Organizers.Users.Shared;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Users.Ban;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, UserBanStateResponse>
{
    public override void Configure()
    {
        Post("organizers/users/{UserId:guid}/ban");
        Description(b => b.WithTags("Organizers", "Users").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Ban a user account";
            s.Description =
                "Allows any organizer or root admin to ban a user globally, with an optional internal reason.";
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

        targetUser.BannedAt = DateTimeOffset.UtcNow;
        targetUser.BanReason = string.IsNullOrWhiteSpace(req.Reason) ? null : req.Reason.Trim();
        targetUser.BannedByUserId = actorUserId.Value;

        await sql.Updateable(targetUser).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new UserBanStateResponse
            {
                UserId = targetUser.Id,
                IsBanned = true,
                BannedAt = targetUser.BannedAt,
                BanReason = targetUser.BanReason,
            },
            ct
        );
    }
}
