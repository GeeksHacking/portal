namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Create;

public class Request
{
    public string Id { get; set; } = null!;
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? Criteria { get; set; }
    public bool IsPublished { get; set; } = false;
}
