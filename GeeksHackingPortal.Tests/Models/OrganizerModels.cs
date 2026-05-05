namespace GeeksHackingPortal.Tests.Models;

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

public class OrganizerInviteResponse
{
    public string Code { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
}

public class AcceptInviteResponse
{
    public Guid ActivityId { get; set; }
    public string Type { get; set; } = "";
}
