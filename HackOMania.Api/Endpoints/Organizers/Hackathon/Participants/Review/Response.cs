namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.Review;

public class Response
{
    public required Guid ParticipantId { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset ReviewedAt { get; init; }
}
