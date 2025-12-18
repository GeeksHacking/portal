using SqlSugar;

namespace HackOMania.Api.Entities;

public class ParticipantReview
{
    public enum ParticipantReviewStatus
    {
        Accepted,
        Rejected,
    }

    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public ParticipantReviewStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    [SugarColumn(IsNullable = true)]
    public string? Reason { get; set; }

    public Guid ParticipantId { get; set; }
}
