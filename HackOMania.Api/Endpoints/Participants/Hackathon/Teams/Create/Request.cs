namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.Create;

/// <summary>
/// Request to create a team
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID or short code
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Team name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Team description (optional)
    /// </summary>
    public string? Description { get; set; }
}
