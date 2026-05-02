using GeeksHackingPortal.Api.Features.Hackathons.GitHubRepositorySettings;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Hackathons;

public class Request
{
    public Guid HackathonId { get; set; }
    public string? Name { get; set; }
    public string? Title
    {
        get => Name;
        set => Name = value;
    }
    public string? Description { get; set; }
    public string? Venue { get; set; }
    public string? Location
    {
        get => Venue;
        set => Venue = value;
    }
    public Uri? HomepageUri { get; set; }
    public string? ShortCode { get; set; }
    public bool? IsPublished { get; set; }
    public DateTimeOffset? EventStartDate { get; set; }
    public DateTimeOffset? StartTime
    {
        get => EventStartDate;
        set => EventStartDate = value;
    }
    public DateTimeOffset? EventEndDate { get; set; }
    public DateTimeOffset? EndTime
    {
        get => EventEndDate;
        set => EventEndDate = value;
    }
    public DateTimeOffset? SubmissionsStartDate { get; set; }
    public DateTimeOffset? ChallengeSelectionEndDate { get; set; }
    public DateTimeOffset? SubmissionsEndDate { get; set; }
    public DateTimeOffset? JudgingStartDate { get; set; }
    public DateTimeOffset? JudgingEndDate { get; set; }
    public Dictionary<string, string>? EmailTemplates { get; set; }
    public HackathonGitHubRepositorySettingsRequest? GitHubRepositorySettings { get; set; }
}
