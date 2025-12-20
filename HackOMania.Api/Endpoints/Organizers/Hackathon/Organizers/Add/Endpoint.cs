using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Organizers.Add;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId}/organizers");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h => h.Id == req.HackathonId)
            .FirstAsync(ct);

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

        await sql.Insertable(
                new Organizer
                {
                    UserId = req.UserId,
                    HackathonId = hackathon.Id,
                    Type = req.Type,
                }
            )
            .ExecuteCommandAsync(ct);

        await Send.OkAsync(new Response { UserId = req.UserId, Type = req.Type }, ct);
    }
}
