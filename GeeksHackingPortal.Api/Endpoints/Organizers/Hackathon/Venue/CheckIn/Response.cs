namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Venue.CheckIn;

public class Response
{
    public required Guid Id { get; set; }
    public required DateTimeOffset CheckInTime { get; set; }
    public required bool IsCheckedIn { get; set; }
}
