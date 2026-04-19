namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Registration.Submissions.Submit;

public class Request
{
    public Guid HackathonId { get; set; }
    public List<SubmissionDto> Submissions { get; set; } = [];
}

public class SubmissionDto
{
    public Guid QuestionId { get; set; }
    public required string Value { get; set; }
    public string? FollowUpValue { get; set; }
}
