using GeeksHackingPortal.Api.Entities;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Organizers.Invite;

public class Request
{
    public Guid StandaloneWorkshopId { get; set; }
    public OrganizerType Type { get; set; } = OrganizerType.Volunteer;
    public DateTimeOffset? ExpiresAt { get; set; }
}
