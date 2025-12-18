namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.JoinTeam;

/// <summary>
/// Request to join a team
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID or short code
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Team ID to join
    /// </summary>
    public string TeamId { get; set; } = null!;

    /// <summary>
    /// Team join code
    /// </summary>
    public required string JoinCode { get; set; }
}
