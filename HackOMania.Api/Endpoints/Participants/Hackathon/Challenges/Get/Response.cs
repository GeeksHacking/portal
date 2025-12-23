namespace HackOMania.Api.Endpoints.Participants.Hackathon.Challenges.Get;

public class Response
{
    public required Guid Id { get; set; }
    public required Guid HackathonId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string SelectionCriteriaStmt { get; set; }
    public required bool IsPublished { get; set; }
}
