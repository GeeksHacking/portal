namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Get;

public class Response
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Venue { get; init; }
    public required Uri HomepageUri { get; init; }
    public required string ShortCode { get; init; }
    public required bool IsPublished { get; init; }
    public required DateTimeOffset EventStartDate { get; init; }
    public required DateTimeOffset EventEndDate { get; init; }
    public required DateTimeOffset SubmissionsStartDate { get; init; }
    public required DateTimeOffset SubmissionsEndDate { get; init; }
    public required DateTimeOffset JudgingStartDate { get; init; }
    public required DateTimeOffset JudgingEndDate { get; init; }
}
