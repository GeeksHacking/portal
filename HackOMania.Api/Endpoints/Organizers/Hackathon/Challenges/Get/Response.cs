namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Get;

public class Response
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string SelectionCriteriaStmt { get; init; }
    public required bool IsPublished { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
