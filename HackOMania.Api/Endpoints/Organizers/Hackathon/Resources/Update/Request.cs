namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Resources.Update;

public class Request
{
    public Guid HackathonId { get; set; }
    public string ResourceId { get; set; } = null!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? RedemptionStmt { get; set; }
    public bool? IsPublished { get; set; }
}
