namespace HackOMania.Api.Endpoints.Participants.Hackathon.Status;

public class Response
{
    public required bool IsParticipant { get; init; }
    public required bool IsOrganizer { get; init; }
    public required Guid? TeamId { get; init; }
    public required string? TeamName { get; init; }
    public required ParticipantStatus? Status { get; init; }
    public string? ReviewReason { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
    public Guid? SelectedChallengeId { get; init; }
    public DateTimeOffset? ChallengeSelectedAt { get; init; }
    public DateTimeOffset? ChallengeSelectionEndDate { get; init; }
}

public enum ParticipantStatus
{
    Pending,
    Accepted,
    Rejected,
}
