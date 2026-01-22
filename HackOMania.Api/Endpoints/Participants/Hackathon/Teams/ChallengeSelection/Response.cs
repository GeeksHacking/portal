namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.ChallengeSelection;

public class Response
{
    public required Guid SelectedChallengeId { get; set; }
    public required DateTimeOffset SelectedAt { get; set; }
}
