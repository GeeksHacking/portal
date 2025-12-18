namespace HackOMania.Api.Endpoints.Participants.Hackathon.Challenges.Get;

public class Response
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Criteria { get; set; }
    public bool IsPublished { get; set; }
}
