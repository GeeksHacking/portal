namespace GeeksHackingPortal.Api.Endpoints.Users.Profile.Update;

public class Response
{
    public required Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Name { get; set; }
}
