using GeeksHackingPortal.Api.Entities;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Registration.Questions.Update;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid QuestionId { get; set; }
    public string? QuestionText { get; set; }
    public QuestionType? Type { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsRequired { get; set; }
    public string? HelpText { get; set; }
    public string? ConditionalLogic { get; set; }
    public string? Category { get; set; }
    public string? ValidationRules { get; set; }
    public List<UpdateOptionDto>? Options { get; set; }
}

public class UpdateOptionDto
{
    public Guid? Id { get; set; }
    public required string OptionText { get; set; }
    public required string OptionValue { get; set; }
    public int DisplayOrder { get; set; }
    public bool HasFollowUpText { get; set; }
    public string? FollowUpPlaceholder { get; set; }
}
