namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.Update;

public class Request
{
    public Guid ActivityId { get; set; }
    public Guid ResourceId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? RedemptionStmt { get; set; }
    public bool? IsPublished { get; set; }
}
