namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.RemoveMember;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
}
