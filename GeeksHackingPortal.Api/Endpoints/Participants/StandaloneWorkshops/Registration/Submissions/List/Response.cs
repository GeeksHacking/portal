namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Registration.Submissions.List;

public class Response
{
    public List<SubmissionDto> Submissions { get; set; } = [];
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
    public int RequiredQuestionsRemaining { get; set; }
}

public class SubmissionDto
{
    public Guid QuestionId { get; set; }
    public string QuestionKey { get; set; } = null!;
    public string QuestionText { get; set; } = null!;
    public string? Category { get; set; }
    public string Value { get; set; } = null!;
    public string? FollowUpValue { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
