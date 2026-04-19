using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Challenges.Delete;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("organizers/hackathons/{HackathonId:guid}/challenges/{ChallengeId:guid}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Challenges"));
        Summary(s =>
        {
            s.Summary = "Delete a challenge";
            s.Description = "Deletes a challenge from the hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var hasExistingSubmissions = await sql.Queryable<ChallengeSubmission>()
            .Where(cs => cs.ChallengeId == req.ChallengeId && cs.HackathonId == hackathon.Id)
            .AnyAsync(ct);

        if (hasExistingSubmissions)
        {
            AddError("Challenges with existing submissions cannot be deleted.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var deleted = await sql.Deleteable<Challenge>()
            .Where(c => c.Id == req.ChallengeId && c.HackathonId == hackathon.Id)
            .ExecuteCommandAsync(ct);

        if (deleted == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
