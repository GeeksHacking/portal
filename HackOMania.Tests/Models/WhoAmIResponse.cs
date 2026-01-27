namespace HackOMania.Tests.Models;

public class WhoAmIResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public long GitHubId { get; set; }
    public string GitHubLogin { get; set; } = "";
    public bool IsRoot { get; set; }
}
