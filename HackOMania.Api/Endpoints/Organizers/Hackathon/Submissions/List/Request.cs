using FastEndpoints;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Submissions.List;

public class Request
{
    public string Id { get; set; } = null!;

    /// <summary>
    /// Optional filter by challenge ID
    /// </summary>
    [QueryParam]
    public Guid? ChallengeId { get; set; }

    /// <summary>
    /// Optional filter by team ID
    /// </summary>
    [QueryParam]
    public Guid? TeamId { get; set; }
}
