using GeeksHackingPortal.Api.Entities;

namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Registration.Questions.List;

public class Response
{
    public List<CategoryDto> Categories { get; set; } = [];
}

public class CategoryDto
{
    public string Name { get; set; } = null!;
    public List<QuestionDto> Questions { get; set; } = [];
}

public class QuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = null!;
    public string QuestionKey { get; set; } = null!;
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public string? HelpText { get; set; }
    public string? ConditionalLogic { get; set; }
    public string? ValidationRules { get; set; }
    public List<OptionDto> Options { get; set; } = [];
    public SubmissionDto? CurrentSubmission { get; set; }
}

public class OptionDto
{
    public Guid Id { get; set; }
    public string OptionText { get; set; } = null!;
    public string OptionValue { get; set; } = null!;
    public bool HasFollowUpText { get; set; }
    public string? FollowUpPlaceholder { get; set; }
}

public class SubmissionDto
{
    public string Value { get; set; } = null!;
    public string? FollowUpValue { get; set; }
}
