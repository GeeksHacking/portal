namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Workshops.WithdrawParticipant;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid WorkshopId { get; set; }
    public Guid UserId { get; set; }
}
