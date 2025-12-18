using HackOMania.Api.Entities;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Organizers.Add;

public class Response
{
    public Guid UserId { get; set; }
    public OrganizerType Type { get; set; }
}
