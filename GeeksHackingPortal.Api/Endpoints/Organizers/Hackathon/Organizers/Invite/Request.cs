using GeeksHackingPortal.Api.Entities;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Organizers.Invite;

public class Request
{
    public Guid HackathonId { get; set; }
    public OrganizerType Type { get; set; } = OrganizerType.Volunteer;
    public DateTimeOffset? ExpiresAt { get; set; }
}
