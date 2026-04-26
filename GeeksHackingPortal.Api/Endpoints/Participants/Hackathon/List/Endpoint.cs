using FastEndpoints;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.List;

public class Endpoint(ISqlSugarClient sql) : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("participants/hackathons");
        AllowAnonymous();
        Description(b => b.WithTags("Participants", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "List all hackathons";
            s.Description = "Retrieves all published hackathons.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var hackathons = await sql.Queryable<Entities.Hackathon>()
            .LeftJoin<Entities.Activity>((hackathon, activity) => hackathon.Id == activity.Id)
            .Where((hackathon, activity) => activity.IsPublished)
            
            .Select((hackathon, activity) => new Response.HackathonItem
            {
                Id = hackathon.Id,
                Name = activity.Title,
                Description = activity.Description,
                Venue = activity.Location,
                HomepageUri = hackathon.HomepageUri,
                ShortCode = hackathon.ShortCode,
                IsPublished = activity.IsPublished,
                EventStartDate = activity.StartTime,
                EventEndDate = activity.EndTime,
                SubmissionsStartDate = hackathon.SubmissionsStartDate,
                ChallengeSelectionEndDate = hackathon.ChallengeSelectionEndDate,
                SubmissionsEndDate = hackathon.SubmissionsEndDate,
                JudgingStartDate = hackathon.JudgingStartDate,
                JudgingEndDate = hackathon.JudgingEndDate,
            })
            .ToListAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Hackathons = hackathons,
            },
            ct
        );
    }
}
