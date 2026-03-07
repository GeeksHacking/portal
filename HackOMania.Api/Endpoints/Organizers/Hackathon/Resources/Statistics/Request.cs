using FastEndpoints;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Resources.Statistics;

public class Request
{
    public Guid HackathonId { get; set; }

    [QueryParam]
    public Guid? ResourceId { get; set; }
}
