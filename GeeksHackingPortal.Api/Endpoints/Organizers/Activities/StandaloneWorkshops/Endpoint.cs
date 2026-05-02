using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/standalone-workshops/{StandaloneWorkshopId:guid}");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Standalone Workshops"));
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

        if (!string.IsNullOrWhiteSpace(req.Title))
        {
            workshop.Activity.Title = req.Title;
        }

        if (!string.IsNullOrWhiteSpace(req.Description))
        {
            workshop.Activity.Description = req.Description;
        }

        if (req.StartTime.HasValue)
        {
            workshop.Activity.StartTime = req.StartTime.Value;
        }

        if (req.EndTime.HasValue)
        {
            workshop.Activity.EndTime = req.EndTime.Value;
        }

        if (!string.IsNullOrWhiteSpace(req.Location))
        {
            workshop.Activity.Location = req.Location;
        }

        if (req.IsPublished.HasValue)
        {
            workshop.Activity.IsPublished = req.IsPublished.Value;
        }

        if (req.HasHomepageUri)
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

        var normalizedTemplates = req.EmailTemplates is null
            ? null
            : EmailTemplateNormalizer.Normalize(req.EmailTemplates);

        await sql.Updateable(workshop.Activity).ExecuteCommandAsync(ct);
        await sql.Updateable(workshop).ExecuteCommandAsync(ct);

        if (normalizedTemplates is not null)
        {
            await sql.Deleteable<HackathonNotificationTemplate>()
                .Where(t => t.ActivityId == workshop.Id)
                .ExecuteCommandAsync(ct);

            if (normalizedTemplates.Count > 0)
            {
                var inserts = normalizedTemplates.Select(kvp => new HackathonNotificationTemplate
                {
                    Id = Guid.NewGuid(),
                    ActivityId = workshop.Id,
                    EventKey = kvp.Key,
                    TemplateId = kvp.Value,
                });
                await sql.Insertable(inserts.ToList()).ExecuteCommandAsync(ct);
            }
        }

        var templates = normalizedTemplates ?? await LoadTemplates(workshop.Id, ct);

        await Send.OkAsync(
            new Response
            {
                Id = workshop.Id,
                Title = workshop.Activity.Title,
                Description = workshop.Activity.Description,
                StartTime = workshop.Activity.StartTime,
                EndTime = workshop.Activity.EndTime,
                Location = workshop.Activity.Location,
                HomepageUri = workshop.HomepageUri,
                ShortCode = workshop.ShortCode,
                MaxParticipants = workshop.MaxParticipants,
                IsPublished = workshop.Activity.IsPublished,
                EmailTemplates = templates,
            },
            ct
        );
    }

    private async Task<Dictionary<string, string>> LoadTemplates(Guid activityId, CancellationToken ct)
    {
        var persisted = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => t.ActivityId == activityId)
            .ToListAsync(ct);
        return persisted
            .GroupBy(t => t.EventKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().TemplateId, StringComparer.OrdinalIgnoreCase);
    }
}
