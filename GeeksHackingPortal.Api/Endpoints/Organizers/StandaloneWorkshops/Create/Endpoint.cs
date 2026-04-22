using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
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

        var emailTemplates = NormalizeEmailTemplates(req.EmailTemplates);
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
            Title = req.Title,
            Description = req.Description,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            Location = req.Location,
            HomepageUri = req.HomepageUri,
            ShortCode = req.ShortCode,
            MaxParticipants = req.MaxParticipants,
            IsPublished = req.IsPublished,
            CreatedAt = now,
            UpdatedAt = now,
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
                Title = workshop.Title,
                Description = workshop.Description,
                StartTime = workshop.StartTime,
                EndTime = workshop.EndTime,
                Location = workshop.Location,
                HomepageUri = workshop.HomepageUri,
                ShortCode = workshop.ShortCode,
                MaxParticipants = workshop.MaxParticipants,
                IsPublished = workshop.IsPublished,
                CreatedAt = workshop.CreatedAt,
                EmailTemplates = emailTemplates,
            },
            ct
        );
    }

    private static Dictionary<string, string> NormalizeEmailTemplates(
        Dictionary<string, string>? templates
    )
    {
        if (templates is null)
        {
            return [];
        }

        return templates
            .Where(kvp =>
                !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value)
            )
            .GroupBy(kvp => kvp.Key.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().Value.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
