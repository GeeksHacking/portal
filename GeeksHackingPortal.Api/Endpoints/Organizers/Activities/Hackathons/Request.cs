using GeeksHackingPortal.Api.Features.Hackathons.GitHubRepositorySettings;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Hackathons;

public class Request
{
    public Guid ActivityId { get; set; }
    public DateTimeOffset? SubmissionsStartDate { get; set; }
    public DateTimeOffset? ChallengeSelectionEndDate { get; set; }
    public DateTimeOffset? SubmissionsEndDate { get; set; }
    public DateTimeOffset? JudgingStartDate { get; set; }
    public DateTimeOffset? JudgingEndDate { get; set; }
    public HackathonGitHubRepositorySettingsRequest? GitHubRepositorySettings { get; set; }
}
