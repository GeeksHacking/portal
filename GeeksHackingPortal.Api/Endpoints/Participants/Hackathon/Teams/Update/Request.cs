namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Teams.Update;

/// <summary>
/// Request to update a team
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID
    /// </summary>
    public Guid HackathonId { get; set; }

    /// <summary>
    /// Team ID to update
    /// </summary>
    public string TeamId { get; set; } = null!;

    /// <summary>
    /// New team name (optional)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// New team description (optional)
    /// </summary>
    public string? Description { get; set; }
}
