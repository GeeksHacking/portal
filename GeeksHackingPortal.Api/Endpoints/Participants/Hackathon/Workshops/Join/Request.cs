namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Workshops.Join;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid WorkshopId { get; set; }
}
