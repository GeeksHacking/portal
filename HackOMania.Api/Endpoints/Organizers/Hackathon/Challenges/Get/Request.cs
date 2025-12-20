namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Get;

public class Request
{
    public Guid HackathonId { get; set; }
    public string ChallengeId { get; set; } = null!;
}
