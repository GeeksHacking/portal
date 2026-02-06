namespace HackOMania.Tests.Models;

public class HackathonResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Venue { get; set; } = "";
    public Uri? HomepageUri { get; set; }
    public string ShortCode { get; set; } = "";
    public bool IsPublished { get; set; }
    public DateTimeOffset EventStartDate { get; set; }
    public DateTimeOffset EventEndDate { get; set; }
    public DateTimeOffset SubmissionsStartDate { get; set; }
    public DateTimeOffset SubmissionsEndDate { get; set; }
    public DateTimeOffset JudgingStartDate { get; set; }
    public DateTimeOffset JudgingEndDate { get; set; }
    public Dictionary<string, string> EmailTemplates { get; set; } = [];
}
