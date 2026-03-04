namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.Withdraw;

public class Request
{
    public required Guid HackathonId { get; init; }
    public required Guid UserId { get; init; }
}
