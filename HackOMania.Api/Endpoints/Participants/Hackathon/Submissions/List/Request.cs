namespace HackOMania.Api.Endpoints.Participants.Hackathon.Submissions.List;

/// <summary>
/// Request to list submissions for a team
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID or short code
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Team ID
    /// </summary>
    public string TeamId { get; set; } = null!;
}
