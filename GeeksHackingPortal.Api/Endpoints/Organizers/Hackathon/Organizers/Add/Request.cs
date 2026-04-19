using GeeksHackingPortal.Api.Entities;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Organizers.Add;

public class Request
{
    public Guid HackathonId { get; set; }
    public required Guid UserId { get; set; }
    public OrganizerType Type { get; set; } = OrganizerType.Volunteer;
}
