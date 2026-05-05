using GeeksHackingPortal.Api.Entities;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Organizers.Invite;

public class Response
{
    public string Code { get; set; } = null!;
    public OrganizerType Type { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
