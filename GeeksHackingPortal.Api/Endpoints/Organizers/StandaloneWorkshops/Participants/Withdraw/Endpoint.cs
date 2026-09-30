using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Participants.Withdraw;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/participants/{UserId:guid}/withdraw");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Activity Participants"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var registration = await sql.Queryable<ActivityRegistration>()
            .InnerJoin<Activity>((r, a) => r.ActivityId == a.Id)
            .Where((r, a) =>
                r.ActivityId == req.StandaloneWorkshopId
                && r.UserId == req.UserId
                && r.Status == ActivityRegistrationStatus.Registered
            )
            .Select((r, a) => new { Registration = r, a.EndTime })
            .FirstAsync(ct);
        if (registration is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (registration.EndTime < DateTimeOffset.UtcNow)
        {
            AddError("You cannot withdraw a participant after the workshop has ended");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        registration.Registration.Status = ActivityRegistrationStatus.Withdrawn;
        registration.Registration.WithdrawnAt = DateTimeOffset.UtcNow;
        await sql.Updateable(registration.Registration).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Message = "Participant has been withdrawn from the standalone workshop",
            },
            ct
        );
    }
}
