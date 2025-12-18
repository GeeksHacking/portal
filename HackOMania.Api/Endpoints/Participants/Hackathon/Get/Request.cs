namespace HackOMania.Api.Endpoints.Participants.Hackathon.Get;

/// <summary>
/// Request to get hackathon details
/// </summary>
public class Request
{
    /// <summary>
    /// Hackathon ID or short code
    /// </summary>
    public string Id { get; set; } = null!;
}
