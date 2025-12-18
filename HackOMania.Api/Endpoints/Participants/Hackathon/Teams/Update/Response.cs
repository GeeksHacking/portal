namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.Update;

public class Response
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
