using GeeksHackingPortal.Api.Entities;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Registration.Questions.Update;

public class Response
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = null!;
    public string QuestionKey { get; set; } = null!;
    public QuestionType Type { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? HelpText { get; set; }
    public string? ConditionalLogic { get; set; }
    public string? Category { get; set; }
    public string? ValidationRules { get; set; }
    public List<OptionResponse> Options { get; set; } = [];
}

public class OptionResponse
{
    public Guid Id { get; set; }
    public string OptionText { get; set; } = null!;
    public string OptionValue { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool HasFollowUpText { get; set; }
    public string? FollowUpPlaceholder { get; set; }
}
