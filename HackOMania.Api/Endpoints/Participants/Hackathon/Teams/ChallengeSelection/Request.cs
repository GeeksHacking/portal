namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.ChallengeSelection;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid TeamId { get; set; }
    public required Guid ChallengeId { get; set; }
}
