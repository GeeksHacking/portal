using GeeksHackingPortal.Api.Features.Hackathons.GitHubRepositorySettings;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Hackathons;

public class Response
{
    public required Guid Id { get; init; }
    public required DateTimeOffset SubmissionsStartDate { get; init; }
    public required DateTimeOffset ChallengeSelectionEndDate { get; init; }
    public required DateTimeOffset SubmissionsEndDate { get; init; }
    public required DateTimeOffset JudgingStartDate { get; init; }
    public required DateTimeOffset JudgingEndDate { get; init; }
    public required HackathonGitHubRepositorySettingsResponse GitHubRepositorySettings { get; init; }
}
