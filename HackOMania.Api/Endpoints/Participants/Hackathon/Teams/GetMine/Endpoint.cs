using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.GetMine;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId}/teams/me");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Teams"));
        Summary(s =>
        {
            s.Summary = "Get my team";
            s.Description = "Retrieves the current user's team for the specified hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await membership.FindHackathon(req.HackathonId, ct);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var participant = await membership.GetParticipant(userId.Value, hackathon.Id, ct);
        if (participant?.TeamId is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var team = await sql.Queryable<Team>()
            .Where(t => t.Id == participant.TeamId && t.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        if (team is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var members = await sql.Queryable<Participant>()
            .InnerJoin<User>((p, u) => p.UserId == u.Id)
            .Where(p => p.HackathonId == hackathon.Id && p.TeamId == team.Id)
            .Select(
                (p, u) =>
                    new Response.Response_Member
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        IsCurrentUser = u.Id == userId,
                    }
            )
            .ToListAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = team.Id,
                HackathonId = team.HackathonId,
                Name = team.Name,
                Description = team.Description,
                JoinCode = team.JoinCode,
                Members = members,
            },
            ct
        );
    }
}
