namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Challenges.Delete;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid ChallengeId { get; set; }
}
