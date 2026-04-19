namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Venue.CheckIn;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid ParticipantUserId { get; set; }
}
