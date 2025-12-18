namespace HackOMania.Api.Endpoints.Participants.Hackathon.Get;

public class Response
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Venue { get; set; }

    public required string HomepageUri { get; set; }

    public required string ShortCode { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset EventStartDate { get; set; }

    public DateTimeOffset EventEndDate { get; set; }

    public DateTimeOffset SubmissionsStartDate { get; set; }

    public DateTimeOffset SubmissionsEndDate { get; set; }

    public DateTimeOffset JudgingStartDate { get; set; }

    public DateTimeOffset JudgingEndDate { get; set; }
}
