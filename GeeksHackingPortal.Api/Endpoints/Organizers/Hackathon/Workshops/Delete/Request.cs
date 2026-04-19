using FastEndpoints;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Workshops.Delete;

public class Request
{
    [BindFrom("HackathonId")]
    public Guid HackathonId { get; set; }

    [BindFrom("WorkshopId")]
    public Guid WorkshopId { get; set; }
}
