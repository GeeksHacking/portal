namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Judges.Create;

public class Request
{
    public Guid HackathonId { get; set; }
    public required string Name { get; set; }
}
