using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

/// <summary>
/// Represents a registration question for a hackathon. Questions can be configured per hackathon.
/// </summary>
public class RegistrationQuestion
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [SugarColumn(OldColumnName = "HackathonId")]
    public Guid ActivityId { get; set; }

    /// <summary>
    /// The question text to display
    /// </summary>
    public string QuestionText { get; set; } = null!;

    /// <summary>
    /// A unique key to identify this question programmatically (e.g., "dietary_restrictions", "tshirt_size")
    /// </summary>
    public string QuestionKey { get; set; } = null!;

    /// <summary>
    /// The type of question (Text, Number, SingleChoice, MultipleChoice, Boolean, Email, Url, Phone)
    /// </summary>
    public QuestionType Type { get; set; }

    /// <summary>
    /// Display order on the registration form
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this question is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Optional help text or description
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string? HelpText { get; set; }

    /// <summary>
    /// Conditional logic - only show if parent question has this answer (JSON: {"questionKey": "value"})
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string? ConditionalLogic { get; set; }

    /// <summary>
    /// Category/section for grouping questions (e.g., "Personal Info", "Professional Background", "Preferences")
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string? Category { get; set; }

    /// <summary>
    /// Validation rules, see <see cref="Services.RegistrationValidationService.ValidationRules"/>
    /// </summary>
    [SugarColumn(IsNullable = true, ColumnDataType = "longtext")]
    public string? ValidationRules { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(ActivityId))]
    public Activity Activity { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(RegistrationQuestionOption.QuestionId))]
    public List<RegistrationQuestionOption> Options { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ParticipantRegistrationSubmission.QuestionId))]
    public List<ParticipantRegistrationSubmission> Submissions { get; set; } = null!;
}

public enum QuestionType
{
    /// <summary>
    /// Short text input
    /// </summary>
    Text,

    /// <summary>
    /// Long text input (textarea)
    /// </summary>
    LongText,

    /// <summary>
    /// Numeric input
    /// </summary>
    Number,

    /// <summary>
    /// Single choice from options (radio buttons)
    /// </summary>
    SingleChoice,

    /// <summary>
    /// Multiple choices from options (checkboxes)
    /// </summary>
    MultipleChoice,

    /// <summary>
    /// Yes/No or True/False
    /// </summary>
    Boolean,

    /// <summary>
    /// Email address with validation
    /// </summary>
    Email,

    /// <summary>
    /// URL with validation
    /// </summary>
    Url,

    /// <summary>
    /// Phone number
    /// </summary>
    Phone,

    /// <summary>
    /// Date picker
    /// </summary>
    Date,

    /// <summary>
    /// Dropdown select
    /// </summary>
    Dropdown,
}
