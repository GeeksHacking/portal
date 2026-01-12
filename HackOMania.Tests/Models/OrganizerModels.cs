namespace HackOMania.Tests.Models;

public class OrganizersListResponse
{
    public IEnumerable<OrganizerItem>? Organizers { get; set; }
}

public class OrganizerItem
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Type { get; set; } = "";
}

public class AddOrganizerResponse
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = "";
}
