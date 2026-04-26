namespace GeeksHackingPortal.Api.Options;

public class AppOptions
{
    public required string FrontendUrl { get; set; }
    public ActivityCreationMode CreationMode { get; set; } = ActivityCreationMode.RootOnly;
    public List<string> AdminEmails { get; set; } = [];
    public List<string> AdminGitHubLogins { get; set; } = [];
}

public enum ActivityCreationMode
{
    RootOnly,
    Anyone,
}
