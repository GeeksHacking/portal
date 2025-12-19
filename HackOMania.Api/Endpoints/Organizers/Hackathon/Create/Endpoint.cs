using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons");
        Policies(PolicyNames.CreateHackathon);
        Description(b => b.WithTags("Organizers", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Create a new hackathon";
            s.Description =
                "Creates a new hackathon event. Permissions depend on the platform configuration.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = new Entities.Hackathon
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Description = req.Description,
            Venue = req.Venue,
            HomepageUri = req.HomepageUri.ToString(),
            ShortCode = req.ShortCode,
            IsPublished = req.IsPublished,
            EventStartDate = req.EventStartDate,
            EventEndDate = req.EventEndDate,
            SubmissionsStartDate = req.SubmissionsStartDate,
            SubmissionsEndDate = req.SubmissionsEndDate,
            JudgingStartDate = req.JudgingStartDate,
            JudgingEndDate = req.JudgingEndDate,
            Organizers =
            [
                new Organizer { UserId = User.GetUserId<Guid>(), Type = OrganizerType.Admin },
            ],
        };

        var ent = await sql.InsertNav(hackathon)
            .Include(h => h.Organizers)
            .ExecuteReturnEntityAsync();

        await Send.CreatedAtAsync<Get.Endpoint>(
            new { Id = ent.Id },
            new Response
            {
                Id = ent.Id,
                Name = ent.Name,
                Description = ent.Description,
                Venue = ent.Venue,
                HomepageUri = ent.HomepageUri,
                ShortCode = ent.ShortCode,
                IsPublished = ent.IsPublished,
                EventStartDate = ent.EventStartDate,
                EventEndDate = ent.EventEndDate,
                SubmissionsStartDate = ent.SubmissionsStartDate,
                SubmissionsEndDate = ent.SubmissionsEndDate,
                JudgingStartDate = ent.JudgingStartDate,
                JudgingEndDate = ent.JudgingEndDate,
            },
            cancellation: ct
        );
    }
}
