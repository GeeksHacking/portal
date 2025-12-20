using FastEndpoints;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.List;

public class Request
{
    public Guid HackathonId { get; set; }

    /// <summary>
    /// Filter by review status: pending, accepted, rejected
    /// </summary>
    [QueryParam]
    public string? Status { get; set; }
}
