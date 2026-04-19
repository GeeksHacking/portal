namespace GeeksHackingPortal.Tests.Models;

public class CreateHackathonRequest
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Venue { get; set; }
    public required Uri HomepageUri { get; set; }
    public required string ShortCode { get; set; }
    public required DateTimeOffset EventStartDate { get; set; }
    public required DateTimeOffset EventEndDate { get; set; }
    public required DateTimeOffset SubmissionsStartDate { get; set; }
    public DateTimeOffset? ChallengeSelectionEndDate { get; set; }
    public required DateTimeOffset SubmissionsEndDate { get; set; }
    public required DateTimeOffset JudgingStartDate { get; set; }
    public required DateTimeOffset JudgingEndDate { get; set; }
    public bool IsPublished { get; set; }
    public Dictionary<string, string>? EmailTemplates { get; set; }
    public GitHubRepositorySettingsRequest? GitHubRepositorySettings { get; set; }
}
