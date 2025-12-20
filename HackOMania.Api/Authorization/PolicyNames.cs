namespace HackOMania.Api.Authorization;

/// <summary>
/// Type-safe policy name constants for authorization.
/// Use these constants with Policies() instead of magic strings.
///
/// <example>
/// Usage in endpoint:
/// <code>
/// public override void Configure()
/// {
///     Get("organizers/hackathons/{HackathonId}");
///     Policies(PolicyNames.OrganizerForHackathon);
/// }
/// </code>
/// </example>
/// </summary>
public static class PolicyNames
{
    /// <summary>
    /// Requires the user to be an organizer for the hackathon specified by {Id} in the route.
    /// The authorization handler will look up the hackathon and verify organizer membership.
    /// </summary>
    public const string OrganizerForHackathon = nameof(OrganizerForHackathon);

    /// <summary>
    /// Requires the user to be a participant for the hackathon specified by {Id} in the route.
    /// The authorization handler will verify the user has joined the hackathon as a participant.
    /// </summary>
    public const string ParticipantForHackathon = nameof(ParticipantForHackathon);

    /// <summary>
    /// Requires the user to be a team member for the team specified by {TeamId} in the route.
    /// Also requires {Id} for the hackathon. Used for team-specific operations.
    /// </summary>
    public const string TeamMemberForHackathonTeam = nameof(TeamMemberForHackathonTeam);

    /// <summary>
    /// Requires the user to be a root/admin user.
    /// </summary>
    public const string Root = nameof(Root);

    /// <summary>
    /// Checks the policy for who can create a hackathon based on AppOptions.
    /// </summary>
    public const string CreateHackathon = nameof(CreateHackathon);
}
