using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Organizers.Invite;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId:guid}/organizers/invites");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Activity Organizers"));
        Summary(s =>
        {
            s.Summary = "Create an organizer invite code for a hackathon";
            s.Description =
                "Generates a single-use invite code that another user can redeem to join the hackathon as an organizer.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var exists = await sql.Queryable<Entities.Hackathon>()
            .AnyAsync(h => h.Id == req.HackathonId, ct);
        if (!exists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var expiresAt = req.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7);
        var code = OrganizerInviteCodeGenerator.Generate();

        var invite = new ActivityOrganizerInvite
        {
            Id = Guid.NewGuid(),
            ActivityId = req.HackathonId,
            Code = code,
            Type = req.Type,
            CreatedByUserId = userId.Value,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
        };

        await sql.Insertable(invite).ExecuteCommandAsync(ct);

        await Send.OkAsync(new Response { Code = code, Type = req.Type, ExpiresAt = expiresAt }, ct);
    }
}
