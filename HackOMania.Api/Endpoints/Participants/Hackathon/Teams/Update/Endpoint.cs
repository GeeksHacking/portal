using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.Update;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("participants/hackathons/{HackathonId}/teams/{TeamId}");
        Policies(PolicyNames.TeamMemberForHackathonTeam);
        Description(b => b.WithTags("Participants", "Teams"));
        Summary(s =>
        {
            s.Summary = "Update team details";
            s.Description = "Updates the team name and description. Only team members can update.";
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

        var team = await sql.Queryable<Team>()
            .Where(t => t.Id.ToString() == req.TeamId && t.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        if (team is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            team.Name = req.Name;
        }

        if (!string.IsNullOrWhiteSpace(req.Description))
        {
            team.Description = req.Description;
        }

        await sql.Updateable(team).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = team.Id,
                HackathonId = team.HackathonId,
                Name = team.Name,
                Description = team.Description,
            },
            ct
        );
    }
}
