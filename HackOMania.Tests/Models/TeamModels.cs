namespace HackOMania.Tests.Models;

public class CreateTeamResponse
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? JoinCode { get; set; }
}

public class MyTeamResponse
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid? ChallengeId { get; set; }
    public string? JoinCode { get; set; }
    public IEnumerable<TeamMemberItem>? Members { get; set; }
}

public class TeamMemberItem
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsCurrentUser { get; set; }
}

public class JoinTeamResponse
{
    public Guid TeamId { get; set; }
    public Guid HackathonId { get; set; }
}

public class JoinTeamByCodeResponse
{
    public Guid TeamId { get; set; }
    public Guid HackathonId { get; set; }
    public bool AutoJoinedHackathon { get; set; }
}

public class LeaveTeamResponse
{
    public string Message { get; set; } = "";
}

public class UpdateTeamResponse
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class SelectChallengeRequest
{
    public Guid ChallengeId { get; set; }
}

public class SelectChallengeResponse
{
    public Guid TeamId { get; set; }
    public Guid? ChallengeId { get; set; }
}
