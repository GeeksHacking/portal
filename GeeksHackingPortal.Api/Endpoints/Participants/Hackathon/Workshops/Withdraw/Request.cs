namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Workshops.Withdraw;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid WorkshopId { get; set; }
}
