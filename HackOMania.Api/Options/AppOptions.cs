namespace HackOMania.Api.Options;

public class AppOptions
{
    public required string FrontendUrl { get; set; }
    public HackathonCreationMode CreationMode { get; set; } = HackathonCreationMode.RootOnly;
    public List<string> AdminEmails { get; set; } = [];
    public List<string> AdminGitHubLogins { get; set; } = [];
}

public enum HackathonCreationMode
{
    RootOnly,
    Anyone,
}
