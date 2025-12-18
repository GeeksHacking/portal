namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.List;

public class Response
{
    public required List<ParticipantItem> Participants { get; init; }
    public required int TotalCount { get; init; }
    public required int PendingCount { get; init; }
    public required int AcceptedCount { get; init; }
    public required int RejectedCount { get; init; }
}

public class ParticipantItem
{
    public required Guid UserId { get; init; }
    public required Guid? TeamId { get; init; }
    public required string? TeamName { get; init; }
    public required ParticipantStatus Status { get; init; }
}

public enum ParticipantStatus
{
    Pending,
    Accepted,
    Rejected,
}
