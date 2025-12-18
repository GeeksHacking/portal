using HackOMania.Api.Entities;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Organizers.Add;

public class Request
{
    public string Id { get; set; } = null!;
    public required Guid UserId { get; set; }
    public OrganizerType Type { get; set; } = OrganizerType.Volunteer;
}
