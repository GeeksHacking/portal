namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Venue.History;

public class Response
{
    public required Guid ParticipantId { get; set; }
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required bool IsCurrentlyCheckedIn { get; set; }
    public required List<HistoryItemDto> History { get; set; }
}

public class HistoryItemDto
{
    public required DateTimeOffset CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public required bool IsCheckedIn { get; set; }
}
