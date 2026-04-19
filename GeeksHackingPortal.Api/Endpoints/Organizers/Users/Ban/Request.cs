namespace GeeksHackingPortal.Api.Endpoints.Organizers.Users.Ban;

public class Request
{
    public Guid UserId { get; set; }
    public string? Reason { get; set; }
}
