namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Participants.List;

public class Response
{
    public required int TotalCount { get; init; }
    public required int PendingCount { get; init; }
    public required int AcceptedCount { get; init; }
    public required int RejectedCount { get; init; }
    public required List<ParticipantItem> Participants { get; init; }
}

public class ParticipantItem
{
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? WithdrawnAt { get; init; }
    public required bool IsWithdrawn { get; init; }
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Email { get; init; }
    public Guid? TeamId { get; init; }
    public string? TeamName { get; init; }
    public required ParticipantConcludedStatus ConcludedStatus { get; init; }
    public required List<ParticipantReviewItem> Reviews { get; init; }
    public required List<RegistrationSubmissionItem> RegistrationSubmissions { get; init; }
    public required int EmailSentCount { get; init; }
    public DateTimeOffset? LastEmailSentAt { get; init; }
    public string? LastEmailStatus { get; init; }
}

public class RegistrationSubmissionItem
{
    public required Guid QuestionId { get; init; }
    public required string QuestionText { get; init; }
    public required string Value { get; init; }
    public string? FollowUpValue { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public class ParticipantReviewItem
{
    public enum ParticipantReviewStatus
    {
        Rejected,
        Accepted,
    }

    public required Guid Id { get; init; }
    public required ParticipantReviewStatus Status { get; init; }
    public required string Reason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public enum ParticipantConcludedStatus
{
    Pending,
    Accepted,
    Rejected,
}
