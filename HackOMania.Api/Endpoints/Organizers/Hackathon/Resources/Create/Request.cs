namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Resources.Create;

public class Request
{
    public Guid HackathonId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string RedemptionStmt { get; set; } = "true";
}
