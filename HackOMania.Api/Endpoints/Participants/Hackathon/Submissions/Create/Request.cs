namespace HackOMania.Api.Endpoints.Participants.Hackathon.Submissions.Create;

public class Request
{
    public string Id { get; set; } = null!;
    public string TeamId { get; set; } = null!;
    public Guid? ChallengeId { get; set; }
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public string? Location { get; set; }
    public Uri? DevpostUri { get; set; }
    public Uri? RepoUri { get; set; }
    public Uri? DemoUri { get; set; }
    public Uri? SlidesUri { get; set; }
}
