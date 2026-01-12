namespace HackOMania.Tests.Models;

public class RegistrationQuestionsResponse
{
    public IEnumerable<CategoryDto>? Categories { get; set; }
}

public class CategoryDto
{
    public string Name { get; set; } = "";
    public IEnumerable<QuestionDto>? Questions { get; set; }
}

public class QuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = "";
    public string QuestionKey { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsRequired { get; set; }
    public string? HelpText { get; set; }
    public string? ConditionalLogic { get; set; }
    public string? ValidationRules { get; set; }
    public IEnumerable<OptionDto>? Options { get; set; }
    public QuestionSubmissionDto? CurrentSubmission { get; set; }
}

public class OptionDto
{
    public Guid Id { get; set; }
    public string OptionText { get; set; } = "";
    public string? OptionValue { get; set; }
    public bool HasFollowUpText { get; set; }
    public string? FollowUpPlaceholder { get; set; }
}

public class QuestionSubmissionDto
{
    public string? Value { get; set; }
    public string? FollowUpValue { get; set; }
}

public class RegistrationSubmissionsResponse
{
    public IEnumerable<RegistrationSubmissionDto>? Submissions { get; set; }
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
    public int RequiredQuestionsRemaining { get; set; }
}

public class RegistrationSubmissionDto
{
    public Guid QuestionId { get; set; }
    public string QuestionKey { get; set; } = "";
    public string QuestionText { get; set; } = "";
    public string? Category { get; set; }
    public string? Value { get; set; }
    public string? FollowUpValue { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class SubmitRegistrationResponse
{
    public int SubmissionsCount { get; set; }
    public string? Message { get; set; }
}
