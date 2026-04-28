using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Analytics;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/analytics");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Standalone Workshops"));
        Summary(s =>
        {
            s.Summary = "Get standalone workshop analytics";
            s.Description = "Returns summary analytics for registrations, check-ins, resources, and templates.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var workshop = await sql.Queryable<StandaloneWorkshop>()
            .InSingleAsync(req.StandaloneWorkshopId);
        if (workshop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var registrations = await sql.Queryable<ActivityRegistration>()
            .Where(r => r.ActivityId == workshop.Id)
            .ToListAsync(ct);
        var checkIns = await sql.Queryable<VenueCheckIn>()
            .Where(v => v.ActivityId == workshop.Id)
            .ToListAsync(ct);

        var registeredCount = registrations.Count(r =>
            r.Status == ActivityRegistrationStatus.Registered && r.WithdrawnAt is null
        );
        var withdrawnCount = registrations.Count(r =>
            r.Status == ActivityRegistrationStatus.Withdrawn || r.WithdrawnAt is not null
        );
        var capacityRemaining = Math.Max(0, workshop.MaxParticipants - registeredCount);
        var capacityUsedPercent = workshop.MaxParticipants > 0
            ? Math.Round(registeredCount * 100d / workshop.MaxParticipants, 1)
            : 0;

        await Send.OkAsync(
            new Response
            {
                RegisteredCount = registeredCount,
                WithdrawnCount = withdrawnCount,
                CapacityRemaining = capacityRemaining,
                CapacityUsedPercent = capacityUsedPercent,
                CheckInCount = checkIns.Count,
                CurrentlyCheckedInCount = checkIns
                    .GroupBy(v => v.ActivityRegistrationId)
                    .Count(g => g.OrderByDescending(v => v.CheckInTime).FirstOrDefault()?.IsCheckedIn == true),
                ResourceCount = await sql.Queryable<Resource>()
                    .Where(r => r.ActivityId == workshop.Id)
                    .CountAsync(ct),
                ResourceRedemptionCount = await sql.Queryable<ResourceRedemption>()
                    .Where(r => r.ActivityId == workshop.Id)
                    .CountAsync(ct),
                EmailTemplateCount = await sql.Queryable<HackathonNotificationTemplate>()
                    .Where(t => t.ActivityId == workshop.Id)
                    .CountAsync(ct),
            },
            ct
        );
    }
}
