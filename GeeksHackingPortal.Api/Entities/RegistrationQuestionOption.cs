using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

/// <summary>
/// Represents a predefined option for SingleChoice or MultipleChoice questions
/// </summary>
public class RegistrationQuestionOption
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }

    /// <summary>
    /// The option text to display
    /// </summary>
    public string OptionText { get; set; } = null!;

    /// <summary>
    /// The value to store when this option is selected
    /// </summary>
    public string OptionValue { get; set; } = null!;

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether selecting this option reveals a follow-up text field
    /// </summary>
    public bool HasFollowUpText { get; set; }

    /// <summary>
    /// Placeholder text for the follow-up field
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string? FollowUpPlaceholder { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(QuestionId))]
    public RegistrationQuestion Question { get; set; } = null!;
}
