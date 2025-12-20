namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.JoinTeam;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid TeamId { get; set; }
    public required string JoinCode { get; set; }
}
