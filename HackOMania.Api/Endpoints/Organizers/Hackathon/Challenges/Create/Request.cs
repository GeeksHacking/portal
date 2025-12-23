namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Create;

public class Request
{
    public required Guid HackathonId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string SelectionCriteriaStmt { get; set; }
    public required bool IsPublished { get; set; }
}
