namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Organizers.Delete;

public class Request
{
    public string Id { get; set; } = null!;
    public Guid UserId { get; set; }
}
