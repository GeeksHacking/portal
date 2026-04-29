namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.List;

public class Response
{
    public required List<ActivityItem> Activities { get; init; }
}

public class ActivityItem
{
    public required Guid Id { get; init; }
    public required string Kind { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Location { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public required bool IsPublished { get; init; }
    public Dictionary<string, string> EmailTemplates { get; init; } = [];
}
