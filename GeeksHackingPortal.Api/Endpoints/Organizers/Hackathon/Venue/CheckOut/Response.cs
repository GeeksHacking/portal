namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Venue.CheckOut;

public class Response
{
    public required Guid Id { get; set; }
    public required DateTimeOffset CheckOutTime { get; set; }
    public required bool IsCheckedIn { get; set; }
}
