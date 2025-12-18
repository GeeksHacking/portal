namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Judges.Create;

public class Request
{
    public string Id { get; set; } = null!;
    public required string Name { get; set; }
}
