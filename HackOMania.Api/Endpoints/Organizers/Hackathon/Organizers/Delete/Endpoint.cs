using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Organizers.Delete;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("organizers/hackathons/{HackathonId:guid}/organizers/{UserId}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var user = await sql.Queryable<Organizer>().Where(o => o.UserId == userId).SingleAsync();
        if (user.Type != OrganizerType.Admin || user.UserId == userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var deleted = await sql.Deleteable<Organizer>()
            .Where(o => o.UserId == req.UserId && o.HackathonId == hackathon.Id)
            .ExecuteCommandAsync(ct);

        if (deleted == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
