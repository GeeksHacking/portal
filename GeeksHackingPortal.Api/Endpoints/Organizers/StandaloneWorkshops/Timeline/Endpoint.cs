using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Timeline;

public class ListRequest
{
    public Guid StandaloneWorkshopId { get; set; }
}

public class MutateRequest
{
    public Guid StandaloneWorkshopId { get; set; }
    public Guid TimelineItemId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}

public class CreateRequest
{
    public Guid StandaloneWorkshopId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
}

public class ListResponse
{
    public required List<TimelineItemDto> TimelineItems { get; set; }
}

public class TimelineItemDto
{
    public Guid Id { get; set; }
    public Guid StandaloneWorkshopId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class ListEndpoint(ISqlSugarClient sql) : Endpoint<ListRequest, ListResponse>
{
    public override void Configure()
    {
        Get("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/timeline");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Standalone Workshops"));
    }

    public override async Task HandleAsync(ListRequest req, CancellationToken ct)
    {
        var exists = await sql.Queryable<StandaloneWorkshop>()
            .AnyAsync(w => w.Id == req.StandaloneWorkshopId, ct);
        if (!exists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var items = await sql.Queryable<EventTimelineItem>()
            .Where(t => t.ActivityId == req.StandaloneWorkshopId)
            .OrderBy(t => t.StartTime)
            .ToListAsync(ct);

        await Send.OkAsync(
            new ListResponse { TimelineItems = [.. items.Select(ToDto)] },
            ct
        );
    }

    private static TimelineItemDto ToDto(EventTimelineItem item) =>
        new()
        {
            Id = item.Id,
            StandaloneWorkshopId = item.ActivityId,
            Title = item.Title,
            Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };
}

public class CreateEndpoint(ISqlSugarClient sql) : Endpoint<CreateRequest, TimelineItemDto>
{
    public override void Configure()
    {
        Post("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/timeline");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Standalone Workshops"));
    }

    public override async Task HandleAsync(CreateRequest req, CancellationToken ct)
    {
        if (req.EndTime <= req.StartTime)
        {
            AddError("EndTime must be after StartTime");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var exists = await sql.Queryable<StandaloneWorkshop>()
            .AnyAsync(w => w.Id == req.StandaloneWorkshopId, ct);
        if (!exists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var item = new EventTimelineItem
        {
            Id = Guid.NewGuid(),
            ActivityId = req.StandaloneWorkshopId,
            Title = req.Title,
            Description = string.IsNullOrWhiteSpace(req.Description) ? string.Empty : req.Description,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await sql.Insertable(item).ExecuteCommandAsync(ct);
        await Send.OkAsync(ToDto(item), ct);
    }

    private static TimelineItemDto ToDto(EventTimelineItem item) =>
        new()
        {
            Id = item.Id,
            StandaloneWorkshopId = item.ActivityId,
            Title = item.Title,
            Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };
}

public class UpdateEndpoint(ISqlSugarClient sql) : Endpoint<MutateRequest, TimelineItemDto>
{
    public override void Configure()
    {
        Patch("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/timeline/{TimelineItemId:guid}");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Standalone Workshops"));
    }

    public override async Task HandleAsync(MutateRequest req, CancellationToken ct)
    {
        var item = await sql.Queryable<EventTimelineItem>()
            .Where(t => t.Id == req.TimelineItemId && t.ActivityId == req.StandaloneWorkshopId)
            .FirstAsync(ct);
        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var startTime = req.StartTime ?? item.StartTime;
        var endTime = req.EndTime ?? item.EndTime;
        if (endTime <= startTime)
        {
            AddError("EndTime must be after StartTime");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(req.Title))
            item.Title = req.Title;
        if (req.Description is not null)
            item.Description = string.IsNullOrWhiteSpace(req.Description) ? string.Empty : req.Description;
        item.StartTime = startTime;
        item.EndTime = endTime;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await sql.Updateable(item).ExecuteCommandAsync(ct);
        await Send.OkAsync(ToDto(item), ct);
    }

    private static TimelineItemDto ToDto(EventTimelineItem item) =>
        new()
        {
            Id = item.Id,
            StandaloneWorkshopId = item.ActivityId,
            Title = item.Title,
            Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };
}

public class DeleteEndpoint(ISqlSugarClient sql) : Endpoint<MutateRequest>
{
    public override void Configure()
    {
        Delete("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/timeline/{TimelineItemId:guid}");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Standalone Workshops"));
    }

    public override async Task HandleAsync(MutateRequest req, CancellationToken ct)
    {
        var item = await sql.Queryable<EventTimelineItem>()
            .Where(t => t.Id == req.TimelineItemId && t.ActivityId == req.StandaloneWorkshopId)
            .FirstAsync(ct);
        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await sql.Deleteable(item).ExecuteCommandAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
