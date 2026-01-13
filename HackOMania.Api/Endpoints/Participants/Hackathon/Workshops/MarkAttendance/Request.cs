namespace HackOMania.Api.Endpoints.Participants.Hackathon.Workshops.MarkAttendance;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid WorkshopId { get; set; }
}
