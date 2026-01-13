namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Workshops.Delete;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid WorkshopId { get; set; }
}
