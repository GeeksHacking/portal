namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.Get;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid UserId { get; set; }
}
