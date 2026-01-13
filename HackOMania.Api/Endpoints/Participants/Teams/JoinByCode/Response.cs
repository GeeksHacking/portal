namespace HackOMania.Api.Endpoints.Participants.Teams.JoinByCode;

public class Response
{
    public Guid TeamId { get; set; }
    public Guid HackathonId { get; set; }
    public bool AutoJoinedHackathon { get; set; }
}
