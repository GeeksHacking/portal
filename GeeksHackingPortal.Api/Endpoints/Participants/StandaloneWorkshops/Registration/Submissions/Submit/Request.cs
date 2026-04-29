namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Registration.Submissions.Submit;

public class Request
{
    public Guid StandaloneWorkshopId { get; set; }
    public List<SubmissionDto> Submissions { get; set; } = [];
}

public class SubmissionDto
{
    public Guid QuestionId { get; set; }
    public required string Value { get; set; }
    public string? FollowUpValue { get; set; }
}
