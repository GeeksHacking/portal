namespace GeeksHackingPortal.Api.Services;

public sealed class GitHubRepositoryAutomationException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
}
