namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Participants.Review;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid ParticipantUserId { get; set; }

    /// <summary>
    /// Review decision: "accept" or "reject"
    /// </summary>
    public required string Decision { get; set; }

    /// <summary>
    /// Reason for the decision. Required for rejections.
    /// </summary>
    public string? Reason { get; set; }
}
