namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Teams.SelectChallenge;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid TeamId { get; set; }
    public Guid ChallengeId { get; set; }
}
