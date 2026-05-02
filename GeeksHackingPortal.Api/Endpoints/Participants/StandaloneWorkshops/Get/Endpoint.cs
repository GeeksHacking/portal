using FastEndpoints;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/standalone-workshops/{StandaloneWorkshopIdOrShortCode}");
        AllowAnonymous();
        Description(b => b.WithTags("Standalone Workshops"));
        Summary(s =>
        {
            s.Summary = "Get standalone workshop details";
            s.Description = "Retrieves public details about a standalone workshop by ID or short code.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var workshop = await sql.Queryable<Entities.StandaloneWorkshop>()
            .Where(w =>
                w.Id.ToString() == req.StandaloneWorkshopIdOrShortCode
                || w.ShortCode == req.StandaloneWorkshopIdOrShortCode
            )
            .Includes(w => w.Activity)
            .FirstAsync(ct);

        if (workshop is null || !workshop.Activity.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(
            new Response
            {
                Id = workshop.Id,
                Title = workshop.Activity.Title,
                Description = workshop.Activity.Description,
                Location = workshop.Activity.Location,
                HomepageUri = workshop.HomepageUri,
                ShortCode = workshop.ShortCode,
                IsPublished = workshop.Activity.IsPublished,
                StartTime = workshop.Activity.StartTime,
                EndTime = workshop.Activity.EndTime,
                MaxParticipants = workshop.MaxParticipants,
            },
            ct
        );
    }
}
