namespace HackOMania.Api.Endpoints.Auth.WhoAmI;

public class Response
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required long GitHubId { get; set; }
    public required string GitHubLogin { get; set; }
    public bool IsRoot { get; set; }
}
