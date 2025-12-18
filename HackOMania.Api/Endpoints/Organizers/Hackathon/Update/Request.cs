namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Update;

/// <summary>
/// Request to update a hackathon
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID
    /// </summary>
    public string Id { get; set; } = null!;

    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Venue { get; set; }
    public Uri? HomepageUri { get; set; }
    public string? ShortCode { get; set; }
    public DateTimeOffset? EventStartDate { get; set; }
    public DateTimeOffset? EventEndDate { get; set; }
    public DateTimeOffset? SubmissionsStartDate { get; set; }
    public DateTimeOffset? SubmissionsEndDate { get; set; }
    public DateTimeOffset? JudgingStartDate { get; set; }
    public DateTimeOffset? JudgingEndDate { get; set; }
    public bool? IsPublished { get; set; }
}
