namespace GeeksHackingPortal.Tests.Models;

public class ActivityResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Location { get; set; } = "";
    public bool IsPublished { get; set; }
    public string Kind { get; set; } = "";
    public Dictionary<string, string> EmailTemplates { get; set; } = [];
}

public class HackathonActivityResponse
{
    public Guid Id { get; set; }
    public DateTimeOffset SubmissionsStartDate { get; set; }
    public DateTimeOffset ChallengeSelectionEndDate { get; set; }
    public DateTimeOffset SubmissionsEndDate { get; set; }
    public DateTimeOffset JudgingStartDate { get; set; }
    public DateTimeOffset JudgingEndDate { get; set; }
    public GitHubRepositorySettingsResponse GitHubRepositorySettings { get; set; } = new();
}

public class StandaloneWorkshopActivityResponse
{
    public Guid Id { get; set; }
    public Uri? HomepageUri { get; set; }
    public string ShortCode { get; set; } = "";
    public int MaxParticipants { get; set; }
}
