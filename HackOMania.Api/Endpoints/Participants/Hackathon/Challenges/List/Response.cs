namespace HackOMania.Api.Endpoints.Participants.Hackathon.Challenges.List;

public class Response
{
    public IEnumerable<ChallengeItem> Challenges { get; set; } = [];

    public class ChallengeItem
    {
        public required Guid Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string SelectionCriteriaStmt { get; init; }
    public required int TeamCount { get; init; }
}
}
