namespace GeeksHackingPortal.Tests.Models;

public class UpdateHackathonRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public string? Location { get; set; }
    public DateTimeOffset? SubmissionsStartDate { get; set; }
    public DateTimeOffset? ChallengeSelectionEndDate { get; set; }
    public DateTimeOffset? SubmissionsEndDate { get; set; }
    public DateTimeOffset? JudgingStartDate { get; set; }
    public DateTimeOffset? JudgingEndDate { get; set; }
    public bool? IsPublished { get; set; }
    public Dictionary<string, string>? EmailTemplates { get; set; }
    public GitHubRepositorySettingsRequest? GitHubRepositorySettings { get; set; }
}
