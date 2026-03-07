using SqlSugar;

namespace HackOMania.Api.Entities;

public class ParticipantReview
{
    public enum ParticipantReviewStatus
    {
        Rejected,
        Accepted,
    }

    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public ParticipantReviewStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string Reason { get; set; } = string.Empty;

    public Guid ParticipantId { get; set; }
}
