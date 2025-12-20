namespace HackOMania.Api.Endpoints.Participants.Hackathon.Join;

/// <summary>
/// Request to join a hackathon
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID or short code
    /// </summary>
    public string HackathonId { get; set; } = null!;
}
