namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.Create;

public class Response
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string JoinCode { get; set; }
}
