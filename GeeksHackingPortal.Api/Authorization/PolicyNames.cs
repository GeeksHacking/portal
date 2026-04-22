namespace GeeksHackingPortal.Api.Authorization;

/// <summary>
/// Type-safe policy name constants for authorization.
/// Use these constants with Policies() instead of magic strings.
///
/// <example>
/// Usage in endpoint:
/// <code>
/// public override void Configure()
/// {
///     Get("organizers/hackathons/{HackathonId:guid}");
///     Policies(PolicyNames.OrganizerForHackathon);
/// }
/// </code>
/// </example>
/// </summary>
public static class PolicyNames
{
    /// <summary>
    /// Requires the user to be an organizer for the hackathon specified by {HackathonId} in the route.
    /// The authorization handler will look up the hackathon and verify organizer membership.
    /// This includes both admins and volunteers.
    ///
    /// TODO - we should distinguish between organizer admins and organizer volunteers.
    /// See <see cref="AdminOrganizersForHackathon"/> and <see cref="AllOrganizersForHackathon"/>
    /// </summary>
    public const string OrganizerForHackathon = nameof(OrganizerForHackathon);

    /// <summary>
    /// Requires the user to be an organizer for the activity specified by {ActivityId},
    /// {StandaloneWorkshopId}, or {HackathonId} in the route.
    /// </summary>
    public const string OrganizerForActivity = nameof(OrganizerForActivity);

    public const string AdminOrganizersForHackathon = nameof(AdminOrganizersForHackathon);
    public const string AllOrganizersForHackathon = nameof(AllOrganizersForHackathon);

    /// <summary>
    /// Requires the user to be a participant for the hackathon specified by {Id} in the route.
    /// The authorization handler will verify the user has joined the hackathon as a participant.
    /// </summary>
    public const string ParticipantForHackathon = nameof(ParticipantForHackathon);

    /// <summary>
    /// Requires the user to be registered for the activity specified by {ActivityId},
    /// {StandaloneWorkshopId}, or {HackathonId} in the route. Activity organizers and root users also pass.
    /// </summary>
    public const string ParticipantForActivity = nameof(ParticipantForActivity);

    /// <summary>
    /// Requires the user to be a team member for the team specified by {TeamId:guid} in the route.
    /// Also requires {Id} for the hackathon. Used for team-specific operations.
    /// </summary>
    public const string TeamMemberForHackathonTeam = nameof(TeamMemberForHackathonTeam);

    /// <summary>
    /// Requires the user to be the creator of the team specified by {TeamId:guid} in the route.
    /// Also requires {HackathonId} for the hackathon. Used for privileged team operations like removing members.
    /// </summary>
    public const string TeamCreatorForHackathonTeam = nameof(TeamCreatorForHackathonTeam);

    /// <summary>
    /// Requires the user to be a root/admin user.
    /// </summary>
    public const string Root = nameof(Root);

    /// <summary>
    /// Checks the policy for who can create a hackathon based on AppOptions.
    /// </summary>
    public const string CreateHackathon = nameof(CreateHackathon);

    /// <summary>
    /// Checks the policy for who can create top-level activities based on AppOptions.
    /// </summary>
    public const string CreateActivity = nameof(CreateActivity);
}
