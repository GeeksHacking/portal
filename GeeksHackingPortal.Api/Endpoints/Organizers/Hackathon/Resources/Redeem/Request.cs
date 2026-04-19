namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.Redeem;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid ParticipantUserId { get; set; }
    public Guid ResourceId { get; set; }
}
