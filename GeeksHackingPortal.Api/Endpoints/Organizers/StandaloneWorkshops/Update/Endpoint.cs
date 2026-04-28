using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/standalone-workshops/{StandaloneWorkshopId:guid}");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Standalone Workshops"));
        Summary(s =>
        {
            s.Summary = "Update standalone workshop details";
            s.Description = "Updates standalone workshop information and notification templates.";
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

        if (!string.IsNullOrWhiteSpace(req.ShortCode) && req.ShortCode != workshop.ShortCode)
        {
            var existingShortCode = await sql.Queryable<StandaloneWorkshop>()
                .AnyAsync(w => w.Id != workshop.Id && w.ShortCode == req.ShortCode, ct);
            if (existingShortCode)
            {
                AddError(r => r.ShortCode, "A standalone workshop with this short code already exists.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }
        }

        using var tran = sql.Ado.UseTran();

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

        if (req.IsPublished.HasValue)
        {
            workshop.Activity.IsPublished = req.IsPublished.Value;
        }

        var emailTemplates = req.EmailTemplates is null
            ? null
            : NormalizeEmailTemplates(req.EmailTemplates);

        if (emailTemplates is not null)
        {
            await sql.Deleteable<HackathonNotificationTemplate>()
                .Where(t => t.ActivityId == workshop.Id)
                .ExecuteCommandAsync(ct);

            if (emailTemplates.Count > 0)
            {
                var notificationTemplates = emailTemplates.Select(kvp => new HackathonNotificationTemplate
                {
                    Id = Guid.NewGuid(),
                    ActivityId = workshop.Id,
                    EventKey = kvp.Key,
                    TemplateId = kvp.Value,
                });

                await sql.Insertable(notificationTemplates.ToList()).ExecuteCommandAsync(ct);
            }
        }

        workshop.Activity.UpdatedAt = DateTimeOffset.UtcNow;
        await sql.Updateable(workshop.Activity).ExecuteCommandAsync(ct);
        await sql.Updateable(workshop).ExecuteCommandAsync(ct);

        tran.CommitTran();

        var persistedTemplates = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => t.ActivityId == workshop.Id)
            .ToListAsync(ct);
        var emailTemplateMap = persistedTemplates
            .GroupBy(t => t.EventKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().TemplateId, StringComparer.OrdinalIgnoreCase);

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
                CreatedAt = workshop.Activity.CreatedAt,
                EmailTemplates = emailTemplateMap,
            },
            ct
        );
    }

    private static Dictionary<string, string> NormalizeEmailTemplates(Dictionary<string, string>? templates)
    {
        if (templates is null)
        {
            return [];
        }

        return templates
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
            .GroupBy(kvp => kvp.Key.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().Value.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
