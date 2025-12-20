namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Organizers.Delete;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid UserId { get; set; }
}
