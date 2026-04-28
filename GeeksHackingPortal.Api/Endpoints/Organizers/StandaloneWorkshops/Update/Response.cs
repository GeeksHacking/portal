namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Update;

public class Response
{
    public required Guid Id { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required DateTimeOffset StartTime { get; set; }

    public required DateTimeOffset EndTime { get; set; }

    public required string Location { get; set; }

    public required Uri HomepageUri { get; set; }

    public required string ShortCode { get; set; }

    public required int MaxParticipants { get; set; }

    public required bool IsPublished { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public required Dictionary<string, string> EmailTemplates { get; set; }
}
