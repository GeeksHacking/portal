namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.Review;

public class Request
{
    public string Id { get; set; } = null!;
    public Guid ParticipantUserId { get; set; }

    /// <summary>
    /// Review decision: "accept" or "reject"
    /// </summary>
    public required string Decision { get; set; }

    /// <summary>
    /// Optional reason for the decision
    /// </summary>
    public string? Reason { get; set; }
}
