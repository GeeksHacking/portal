namespace GeeksHackingPortal.Tests.Models;

public class ChallengeDetailResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? SelectionCriteriaStmt { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class ChallengeUpdateResponse
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Criteria { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class CreateChallengeRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? SelectionCriteriaStmt { get; set; }
    public bool IsPublished { get; set; }
}
