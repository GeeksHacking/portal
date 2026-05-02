using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Withdraw;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/standalone-workshops/{StandaloneWorkshopId:guid}/withdraw");
        Policies(PolicyNames.ParticipantForActivity);
        Description(b => b.WithTags("Standalone Workshops").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Withdraw from a standalone workshop";
            s.Description = "Withdraws the current user from a standalone workshop registration.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var workshop = await sql.Queryable<StandaloneWorkshop>()
            .Includes(w => w.Activity)
            .InSingleAsync(req.StandaloneWorkshopId);
        if (workshop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (workshop.Activity.EndTime < DateTimeOffset.UtcNow)
        {
            AddError("You cannot leave a workshop after it has ended");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var registration = await sql.Queryable<ActivityRegistration>()
            .FirstAsync(
                r =>
                    r.ActivityId == workshop.Id
                    && r.UserId == userId.Value
                    && r.Status == ActivityRegistrationStatus.Registered
                    && r.WithdrawnAt == null,
                ct
            );
        if (registration is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        registration.Status = ActivityRegistrationStatus.Withdrawn;
        registration.WithdrawnAt = DateTimeOffset.UtcNow;

        await sql.Updateable(registration).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response { Message = "You have withdrawn from the workshop" },
            ct
        );
    }
}
