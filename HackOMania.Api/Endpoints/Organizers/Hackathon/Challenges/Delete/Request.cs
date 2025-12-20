namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Delete;

/// <summary>
/// Request to delete a challenge
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID
    /// </summary>
    public Guid HackathonId { get; set; }

    /// <summary>
    /// Challenge ID to delete
    /// </summary>
    public string ChallengeId { get; set; } = null!;
}
