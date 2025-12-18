namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.GetMine;

/// <summary>
/// Request to get the current user's team
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID or short code
    /// </summary>
    public string Id { get; set; } = null!;
}
