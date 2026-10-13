namespace GeeksHackingPortal.Tests.Models;

public class ActivityFileResponse
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Url { get; set; } = "";
}

public class OrganizerActivityFilesResponse
{
    public ActivityFileResponse? Banner { get; set; }
    public List<ActivityFileResponse> Files { get; set; } = [];
}

public class PublicActivityFilesResponse
{
    public List<ActivityFileResponse> Files { get; set; } = [];
}

public class PublicStandaloneWorkshopResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? BannerUrl { get; set; }
}

public class PublicHackathonResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? BannerUrl { get; set; }
}

public class ParticipantWorkshopBannerListResponse
{
    public List<ParticipantWorkshopBannerItem> Workshops { get; set; } = [];
}

public class ParticipantWorkshopBannerItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? BannerUrl { get; set; }
}
