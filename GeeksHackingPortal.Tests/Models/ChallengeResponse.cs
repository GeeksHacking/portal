namespace GeeksHackingPortal.Tests.Models;

public class ChallengeResponse
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Criteria { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ChallengesListResponse
{
    public IEnumerable<ChallengeItem>? Challenges { get; set; }

    public class ChallengeItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string? Criteria { get; set; }
        public bool IsPublished { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
