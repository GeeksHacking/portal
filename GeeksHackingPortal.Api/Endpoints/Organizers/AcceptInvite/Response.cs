using GeeksHackingPortal.Api.Entities;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.AcceptInvite;

public class Response
{
    public Guid ActivityId { get; set; }
    public OrganizerType Type { get; set; }
}
