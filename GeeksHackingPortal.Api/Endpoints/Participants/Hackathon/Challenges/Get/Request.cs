namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Challenges.Get;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid ChallengeId { get; set; }
}
