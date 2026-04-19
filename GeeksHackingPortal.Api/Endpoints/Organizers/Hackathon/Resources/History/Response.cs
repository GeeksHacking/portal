namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.History;

public class Response
{
    public required Guid ParticipantId { get; set; }
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required Guid ResourceId { get; set; }
    public required string ResourceName { get; set; }
    public required bool ResourceIsPublished { get; set; }
    public required bool HasRedeemed { get; set; }
    public required int RedemptionCount { get; set; }
    public required List<HistoryItemDto> History { get; set; }
}

public class HistoryItemDto
{
    public required Guid RedemptionId { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
