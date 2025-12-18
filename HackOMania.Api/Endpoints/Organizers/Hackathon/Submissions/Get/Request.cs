namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Submissions.Get;

public class Request
{
    public string Id { get; set; } = null!;
    public Guid SubmissionId { get; set; }
}
