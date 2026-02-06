namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.BatchEmail;

public class Request
{
    public Guid HackathonId { get; set; }

    /// <summary>
    /// Filter participants by review status. Options: "Accepted", "Rejected", or "All"
    /// </summary>
    public string Status { get; set; } = "All";

    /// <summary>
    /// List of specific participant user IDs to send emails to (optional)
    /// If provided, only these participants will receive emails
    /// </summary>
    public List<Guid>? ParticipantUserIds { get; set; }
}
