namespace HackOMania.Api.Endpoints.Participants.Hackathon.Submissions.Create;

public class Response
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public Guid TeamId { get; set; }
    public Guid? ChallengeId { get; set; }
    public string Title { get; set; } = null!;
    public string? Summary { get; set; }
    public string? Location { get; set; }
    public Uri? DevpostUri { get; set; }
    public Uri? RepoUri { get; set; }
    public Uri? DemoUri { get; set; }
    public Uri? SlidesUri { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}
