namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Submissions.Create;

public class Response
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public Guid TeamId { get; set; }
    public Guid ChallengeId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required Uri RepoUri { get; set; }
    public required Uri DemoUri { get; set; }
    public required Uri SlidesUri { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}
