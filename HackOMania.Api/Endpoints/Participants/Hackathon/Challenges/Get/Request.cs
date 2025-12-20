namespace HackOMania.Api.Endpoints.Participants.Hackathon.Challenges.Get;

/// <summary>
/// Request to get challenge details
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID
    /// </summary>
    public Guid HackathonId { get; set; }

    /// <summary>
    /// Challenge ID
    /// </summary>
    public string ChallengeId { get; set; } = null!;
}
