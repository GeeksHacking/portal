namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Venue.Overview;

public class Response
{
    public required List<ParticipantCheckInDto> Participants { get; set; }
}

public class ParticipantCheckInDto
{
    public required Guid ParticipantId { get; set; }
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required bool IsCurrentlyCheckedIn { get; set; }
    public DateTimeOffset? LastCheckInTime { get; set; }
    public DateTimeOffset? LastCheckOutTime { get; set; }
    public required int TotalCheckIns { get; set; }
}
