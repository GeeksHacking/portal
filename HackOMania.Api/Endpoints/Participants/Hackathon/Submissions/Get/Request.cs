namespace HackOMania.Api.Endpoints.Participants.Hackathon.Submissions.Get;

public class Request
{
    public string Id { get; set; } = null!;
    public Guid SubmissionId { get; set; }
}
