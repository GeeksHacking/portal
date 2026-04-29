namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Organizers.List;

public class Response
{
    public IEnumerable<OrganizerItem> Organizers { get; set; } = [];

    public class OrganizerItem
    {
        public required Guid UserId { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Type { get; set; }
    }
}
