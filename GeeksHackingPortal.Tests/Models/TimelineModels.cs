namespace GeeksHackingPortal.Tests.Models;

public class TimelineItemResponse
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class TimelineListResponse
{
    public List<TimelineItemDto> TimelineItems { get; set; } = new();
}

public class TimelineItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
}
