namespace GeeksHackingPortal.Tests.Models;

public class ParticipantChallengesListResponse
{
    public IEnumerable<ParticipantChallengeItem>? Challenges { get; set; }
}

public class ParticipantChallengeItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? SelectionCriteriaStmt { get; set; }
    public int TeamCount { get; set; }
}

public class ParticipantChallengeResponse
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? SelectionCriteriaStmt { get; set; }
    public bool IsPublished { get; set; }
}
