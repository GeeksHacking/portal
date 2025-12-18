namespace HackOMania.Api.Endpoints.Participants.Hackathon.Challenges.Get;

/// <summary>
/// Request to get challenge details
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID or short code
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Challenge ID
    /// </summary>
    public string ChallengeId { get; set; } = null!;
}
