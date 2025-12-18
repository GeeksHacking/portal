namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Delete;

/// <summary>
/// Request to delete a challenge
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Challenge ID to delete
    /// </summary>
    public string ChallengeId { get; set; } = null!;
}
