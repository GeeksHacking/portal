namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Venue.History;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid ParticipantUserId { get; set; }
}
