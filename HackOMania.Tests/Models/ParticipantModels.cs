namespace HackOMania.Tests.Models;

public class ParticipantsListResponse
{
    public IEnumerable<ParticipantItem>? Participants { get; set; }
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
}

public class ParticipantItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid? TeamId { get; set; }
    public string? TeamName { get; set; }
    public string ConcludedStatus { get; set; } = "";
    public IEnumerable<ParticipantReviewItem>? Reviews { get; set; }
}

public class ParticipantReviewItem
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ParticipantReviewRequest
{
    public string Decision { get; set; } = "";
    public string? Reason { get; set; }
}

public class ParticipantReviewResponse
{
    public Guid ParticipantId { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset ReviewedAt { get; set; }
}

public class BatchEmailRequest
{
    public string Status { get; set; } = "All";
    public List<Guid>? ParticipantUserIds { get; set; }
}

public class BatchEmailResponse
{
    public int TotalEmailsSent { get; set; }
    public int AcceptedEmailsSent { get; set; }
    public int RejectedEmailsSent { get; set; }
    public List<string> Errors { get; set; } = [];
}
