namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Create;

public class Response
{
    public required Guid Id { get; set; }
    public required Guid HackathonId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Sponsor { get; set; }
    public required string Criteria { get; set; }
    public required bool IsPublished { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
