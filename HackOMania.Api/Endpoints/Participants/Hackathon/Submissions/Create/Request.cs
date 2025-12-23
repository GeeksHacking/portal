namespace HackOMania.Api.Endpoints.Participants.Hackathon.Submissions.Create;

public class Request
{
    public required Guid HackathonId { get; set; }
    public required Guid TeamId { get; set; }
    public required Guid ChallengeId { get; set; }
    public required string Title { get; set; }
    public required string Summary { get; set; }
    public required string Location { get; set; }
    public required Uri DevpostUri { get; set; }
    public required Uri RepoUri { get; set; }
    public required Uri DemoUri { get; set; }
    public required Uri SlidesUri { get; set; }
}
