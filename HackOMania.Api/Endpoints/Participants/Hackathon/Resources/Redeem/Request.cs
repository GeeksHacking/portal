namespace HackOMania.Api.Endpoints.Participants.Hackathon.Resources.Redeem;

public class Request
{
    // Hackathon identifier (GUID or short code) from route
    public string Id { get; set; } = null!;

    // Resource identifier from route (GUID string)
    public string ResourceId { get; set; } = null!;
}
