namespace HackOMania.Api.Endpoints.Auth.Impersonate;

public class Request
{
    public required long GitHubId { get; set; }
    public required string GitHubLogin { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
}