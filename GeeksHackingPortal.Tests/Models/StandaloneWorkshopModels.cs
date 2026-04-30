namespace GeeksHackingPortal.Tests.Models;

public class CreateStandaloneWorkshopRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public required string Location { get; set; }
    public Uri? HomepageUri { get; set; }
    public required string ShortCode { get; set; }
    public required int MaxParticipants { get; set; }
    public bool IsPublished { get; set; }
    public Dictionary<string, string>? EmailTemplates { get; set; }
}

public class StandaloneWorkshopResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Location { get; set; } = "";
    public Uri? HomepageUri { get; set; }
    public string ShortCode { get; set; } = "";
    public int MaxParticipants { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Dictionary<string, string> EmailTemplates { get; set; } = [];
}

public class StandaloneWorkshopListResponse
{
    public List<StandaloneWorkshopResponse> StandaloneWorkshops { get; set; } = [];
}

public class StandaloneWorkshopAnalyticsResponse
{
    public int RegisteredCount { get; set; }
    public int WithdrawnCount { get; set; }
    public int CapacityRemaining { get; set; }
    public double CapacityUsedPercent { get; set; }
    public int CheckInCount { get; set; }
    public int CurrentlyCheckedInCount { get; set; }
    public int ResourceCount { get; set; }
    public int ResourceRedemptionCount { get; set; }
    public int EmailTemplateCount { get; set; }
}

public class StandaloneWorkshopJoinResponse
{
    public Guid StandaloneWorkshopId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}

public class StandaloneWorkshopStatusResponse
{
    public bool IsRegistered { get; set; }
    public bool IsOrganizer { get; set; }
    public DateTimeOffset? RegisteredAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }
}

public class StandaloneWorkshopWithdrawResponse
{
    public string Message { get; set; } = "";
}
