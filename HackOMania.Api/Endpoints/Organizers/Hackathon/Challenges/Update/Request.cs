namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Update;

public class Request
{
    public Guid HackathonId { get; set; }
    public string ChallengeId { get; set; } = null!;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Criteria { get; set; }
    public bool? IsPublished { get; set; }
}
