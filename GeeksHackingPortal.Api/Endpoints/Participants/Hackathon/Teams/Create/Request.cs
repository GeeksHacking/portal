namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Teams.Create;

public class Request
{
    public Guid HackathonId { get; set; }

    public required string Name { get; set; }
    public string Description { get; set; } = "";
}
