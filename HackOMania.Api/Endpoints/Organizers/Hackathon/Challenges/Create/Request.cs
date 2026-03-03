namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Create;

public class Request
{
    public Guid HackathonId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Sponsor { get; set; }

    /// <summary>
    /// Jint boolean expression for team eligibility to select the challenge.
    /// Available variables: challenge, teamSize, currentTeamsInChallenge, totalParticipants,
    /// totalSubmissions (alias of currentTeamsInChallenge).
    /// </summary>
    public string? SelectionCriteriaStmt { get; set; }
    public bool IsPublished { get; set; }
}
