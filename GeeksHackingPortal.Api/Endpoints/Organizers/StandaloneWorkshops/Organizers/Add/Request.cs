using GeeksHackingPortal.Api.Entities;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Organizers.Add;

public class Request
{
    public Guid StandaloneWorkshopId { get; set; }
    public required Guid UserId { get; set; }
    public OrganizerType Type { get; set; } = OrganizerType.Volunteer;
}
