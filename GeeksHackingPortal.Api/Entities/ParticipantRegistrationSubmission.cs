using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

/// <summary>
/// Stores a participant's submission for a registration question
/// </summary>
[SugarIndex(
    "IX_ParticipantRegistrationSubmission_Participant_Question",
    nameof(ParticipantId),
    OrderByType.Asc,
    nameof(QuestionId),
    OrderByType.Asc,
    true
)]
public class ParticipantRegistrationSubmission
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid ParticipantId { get; set; }
    public Guid QuestionId { get; set; }

    /// <summary>
    /// The answer value. For multiple choice, this is a JSON array of selected values.
    /// </summary>
    public string Value { get; set; } = null!;

    /// <summary>
    /// For options with follow-up text (e.g., "Other - please specify"), this stores that text
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string? FollowUpValue { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Navigate(NavigateType.OneToOne, nameof(QuestionId))]
    public RegistrationQuestion Question { get; set; } = null!;
}
