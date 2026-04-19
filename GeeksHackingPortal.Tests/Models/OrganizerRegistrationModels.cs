namespace GeeksHackingPortal.Tests.Models;

public class OrganizerRegistrationQuestionsResponse
{
    public IEnumerable<OrganizerQuestionDto>? Questions { get; set; }
}

public class OrganizerQuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = "";
    public string QuestionKey { get; set; } = "";
    public string Type { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? HelpText { get; set; }
    public string? ConditionalLogic { get; set; }
    public string? Category { get; set; }
    public string? ValidationRules { get; set; }
    public IEnumerable<OrganizerOptionDto>? Options { get; set; }
}

public class OrganizerOptionDto
{
    public Guid Id { get; set; }
    public string OptionText { get; set; } = "";
    public string? OptionValue { get; set; }
    public int DisplayOrder { get; set; }
    public bool HasFollowUpText { get; set; }
    public string? FollowUpPlaceholder { get; set; }
}

public class CreateRegistrationQuestionResponse
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = "";
    public string QuestionKey { get; set; } = "";
    public string Type { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? HelpText { get; set; }
    public string? Category { get; set; }
}
