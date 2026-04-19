namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.Overview;

public class Response
{
    public required Guid ResourceId { get; set; }
    public required string ResourceName { get; set; }
    public required bool IsPublished { get; set; }
    public required int TotalRedemptions { get; set; }
    public required int UniqueRedeemers { get; set; }
    public required List<ParticipantResourceRedemptionDto> Participants { get; set; }
    public required List<ResourceAuditTrailItemDto> AuditTrail { get; set; }
}

public class ParticipantResourceRedemptionDto
{
    public required Guid ParticipantId { get; set; }
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required bool HasRedeemed { get; set; }
    public required int RedemptionCount { get; set; }
    public DateTimeOffset? LastRedeemedAt { get; set; }
}

public class ResourceAuditTrailItemDto
{
    public required Guid RedemptionId { get; set; }
    public required Guid ParticipantId { get; set; }
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}
