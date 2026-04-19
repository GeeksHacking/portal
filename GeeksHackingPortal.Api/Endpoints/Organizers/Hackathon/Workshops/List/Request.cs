using FastEndpoints;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Workshops.List;

public class Request
{
    [BindFrom("HackathonId")]
    public Guid HackathonId { get; set; }
}
