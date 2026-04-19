namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Teams.List;

public class Response
{
    public required List<TeamItem> Teams { get; init; }
}

public class TeamItem
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public Guid? ChallengeId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required int MemberCount { get; init; }
}
