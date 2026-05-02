using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Challenges.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId:guid}/challenges");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Challenges"));
        Summary(s =>
        {
            s.Summary = "Create a challenge";
            s.Description = "Creates a new challenge for the hackathon..";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathonId = req.HackathonId;

        var challenge = new Challenge
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            Title = req.Title,
            Description = req.Description,
            Sponsor = req.Sponsor,
            SelectionCriteriaStmt = req.SelectionCriteriaStmt ?? "true",
            IsPublished = req.IsPublished,
        };

        await sql.Insertable(challenge).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = challenge.Id,
                HackathonId = challenge.HackathonId,
                Title = challenge.Title,
                Description = challenge.Description,
                Sponsor = challenge.Sponsor,
                Criteria = challenge.SelectionCriteriaStmt,
                IsPublished = challenge.IsPublished,
                CreatedAt = challenge.CreatedAt,
            },
            ct
        );
    }
}
