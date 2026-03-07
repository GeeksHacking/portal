namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Resources.Update;

public class Response
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string RedemptionStmt { get; set; } = "true";
    public bool IsPublished { get; set; }
}
