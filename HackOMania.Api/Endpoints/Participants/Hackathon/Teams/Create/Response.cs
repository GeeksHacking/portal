namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.Create;

public class Response
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string JoinCode { get; set; }
}
