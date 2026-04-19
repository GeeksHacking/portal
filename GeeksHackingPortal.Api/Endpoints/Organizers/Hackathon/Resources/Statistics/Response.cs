namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.Statistics;

public class Response
{
    public Guid? ResourceId { get; set; }
    public string? ResourceName { get; set; }
    public required int ResourceCount { get; set; }
    public required int ResourcesWithRedemptions { get; set; }
    public required int ResourcesWithoutRedemptions { get; set; }
    public required int TotalParticipants { get; set; }
    public required int ParticipantsWithRedemptions { get; set; }
    public required int ParticipantsWithoutRedemptions { get; set; }
    public required int TeamsWithRedemptions { get; set; }
    public required int RedeemersWithoutTeam { get; set; }
    public required int TotalRedemptions { get; set; }
    public required decimal AverageRedemptionsPerRedeemer { get; set; }
    public DateTimeOffset? FirstRedeemedAt { get; set; }
    public DateTimeOffset? LastRedeemedAt { get; set; }
    public required List<ResourceSummaryItem> ResourceSummaries { get; set; }
    public required List<TeamBreakdownItem> TeamBreakdown { get; set; }
    public required List<RecentRedemptionItem> RecentActivity { get; set; }
}

public class ResourceSummaryItem
{
    public required Guid ResourceId { get; set; }
    public required string ResourceName { get; set; }
    public required bool IsPublished { get; set; }
    public required int TotalRedemptions { get; set; }
    public required int UniqueRedeemers { get; set; }
    public DateTimeOffset? LastRedeemedAt { get; set; }
}

public class TeamBreakdownItem
{
    public Guid? TeamId { get; set; }
    public required string TeamName { get; set; }
    public required int MemberCount { get; set; }
    public required int RedeemerCount { get; set; }
    public required int TotalRedemptions { get; set; }
    public required int DistinctResourcesRedeemed { get; set; }
    public DateTimeOffset? LastRedeemedAt { get; set; }
    public required List<ParticipantBreakdownItem> Participants { get; set; }
}

public class ParticipantBreakdownItem
{
    public required Guid ParticipantId { get; set; }
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required int RedemptionCount { get; set; }
    public required int DistinctResourcesRedeemed { get; set; }
    public DateTimeOffset? FirstRedeemedAt { get; set; }
    public DateTimeOffset? LastRedeemedAt { get; set; }
    public required List<ParticipantRedemptionEventItem> Redemptions { get; set; }
}

public class ParticipantRedemptionEventItem
{
    public required Guid RedemptionId { get; set; }
    public required Guid ResourceId { get; set; }
    public required string ResourceName { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}

public class RecentRedemptionItem
{
    public required Guid RedemptionId { get; set; }
    public required Guid ResourceId { get; set; }
    public required string ResourceName { get; set; }
    public required Guid ParticipantId { get; set; }
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public Guid? TeamId { get; set; }
    public required string TeamName { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}
