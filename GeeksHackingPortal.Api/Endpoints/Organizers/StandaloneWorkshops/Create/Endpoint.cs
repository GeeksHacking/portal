using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Endpoints.Organizers.Activities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/standalone-workshops");
        Policies(PolicyNames.CreateActivity);
        Description(b => b.WithTags("Organizers", "Standalone Workshops"));
        Summary(s =>
        {
            s.Summary = "Create a standalone workshop";
            s.Description = "Creates an activity-backed workshop independent of a hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var existingShortCode = await sql.Queryable<StandaloneWorkshop>()
            .AnyAsync(workshop => workshop.ShortCode == req.ShortCode, ct);

        if (existingShortCode)
        {
            AddError(r => r.ShortCode, "A standalone workshop with this short code already exists.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var emailTemplates = EmailTemplateNormalizer.Normalize(req.EmailTemplates);
        var workshopId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var activity = new Activity
        {
            Id = workshopId,
            Kind = ActivityKind.StandaloneWorkshop,
            Title = req.Title,
            Description = req.Description,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            Location = req.Location,
            IsPublished = req.IsPublished,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var workshop = new StandaloneWorkshop
        {
            Id = workshopId,
            Activity = activity,
            HomepageUri = req.HomepageUri,
            ShortCode = req.ShortCode,
            MaxParticipants = req.MaxParticipants,
        };
        var organizer = new ActivityOrganizer
        {
            Id = Guid.NewGuid(),
            ActivityId = activity.Id,
            UserId = userId.Value,
            Type = OrganizerType.Admin,
            CreatedAt = now,
        };
        var notificationTemplates = emailTemplates
            .Select(kvp => new HackathonNotificationTemplate
            {
                Id = Guid.NewGuid(),
                ActivityId = activity.Id,
                EventKey = kvp.Key,
                TemplateId = kvp.Value,
            })
            .ToList();

        var transactionResult = await sql.Ado.UseTranAsync(async () =>
        {
            await sql.Insertable(activity).ExecuteCommandAsync(ct);
            await sql.Insertable(workshop).ExecuteCommandAsync(ct);
            await sql.Insertable(organizer).ExecuteCommandAsync(ct);
            if (notificationTemplates.Count > 0)
            {
                await sql.Insertable(notificationTemplates).ExecuteCommandAsync(ct);
            }
        });

        if (!transactionResult.IsSuccess)
        {
            throw transactionResult.ErrorException!;
        }

        await Send.OkAsync(
            new Response
            {
                Id = workshop.Id,
                Title = activity.Title,
                Description = activity.Description,
                StartTime = activity.StartTime,
                EndTime = activity.EndTime,
                Location = activity.Location,
                HomepageUri = workshop.HomepageUri,
                ShortCode = workshop.ShortCode,
                MaxParticipants = workshop.MaxParticipants,
                IsPublished = activity.IsPublished,
                CreatedAt = activity.CreatedAt,
                EmailTemplates = emailTemplates,
            },
            ct
        );
    }
}
