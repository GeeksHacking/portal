namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.GetMine;

public class Response
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public Guid? ChallengeId { get; init; }
    public required string JoinCode { get; init; }
    public required List<MemberItem> Members { get; init; }

    public class MemberItem
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsCurrentUser { get; set; }
    }
}
