using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.StandaloneWorkshops;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/activities/{ActivityId:guid}/standalone-workshop");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Activities", "Standalone Workshops"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var workshop = await sql.Queryable<StandaloneWorkshop>().InSingleAsync(req.ActivityId);
        if (workshop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(req.ShortCode) && req.ShortCode != workshop.ShortCode)
        {
            var exists = await sql.Queryable<StandaloneWorkshop>()
                .AnyAsync(w => w.Id != workshop.Id && w.ShortCode == req.ShortCode, ct);
            if (exists)
            {
                AddError(r => r.ShortCode, "A standalone workshop with this short code already exists.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }
        }

        if (req.HomepageUri is not null)
        {
            workshop.HomepageUri = req.HomepageUri;
        }

        if (!string.IsNullOrWhiteSpace(req.ShortCode))
        {
            workshop.ShortCode = req.ShortCode;
        }

        if (req.MaxParticipants.HasValue)
        {
            workshop.MaxParticipants = req.MaxParticipants.Value;
        }

        await sql.Updateable(workshop).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = workshop.Id,
                HomepageUri = workshop.HomepageUri,
                ShortCode = workshop.ShortCode,
                MaxParticipants = workshop.MaxParticipants,
            },
            ct
        );
    }
}
