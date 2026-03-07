namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Resources.Create;

public class Request
{
    public Guid HackathonId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Jint boolean expression for redemption eligibility.
    /// Available variables: resource, participantRedemptions, teamRedemptions, teamSize,
    /// totalRedemptions, hasTeam.
    /// </summary>
    public string RedemptionStmt { get; set; } = "true";
    public bool IsPublished { get; set; }
}
