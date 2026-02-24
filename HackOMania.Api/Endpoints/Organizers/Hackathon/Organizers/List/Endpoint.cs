using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Organizers.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/organizers");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers"));
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

        var organizers = await sql.Queryable<Organizer>()
            .InnerJoin<User>((o, u) => o.UserId == u.Id)
            .Where(o => o.HackathonId == hackathon.Id)
            .WithCache()
            .Select(
                (o, u) =>
                    new Response.OrganizerItem
                    {
                        UserId = u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        Email = u.Email,
                        Type = o.Type.ToString(),
                    }
            )
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Organizers = organizers }, ct);
    }
}
