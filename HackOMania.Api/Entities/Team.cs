using System.Security.Cryptography;
using SqlSugar;

namespace HackOMania.Api.Entities;

/// <summary>
/// Team and project information
/// </summary>
[SugarIndex("IX_Team_JoinCode", nameof(JoinCode), OrderByType.Asc, IsUnique = true)]
public class Team
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    /// <summary>
    /// Team / project name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Team / project description
    /// </summary>
    [SugarColumn(ColumnDataType = "longtext")]
    public string Description { get; set; } = null!;

    /// <summary>
    /// Secret code to join team
    /// </summary>
    public string JoinCode { get; set; } =
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public Guid HackathonId { get; set; }
    public Guid CreatedByUserId { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? ChallengeId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Navigate(NavigateType.OneToMany, nameof(Participant.TeamId))]
    public List<Participant> Members { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ChallengeSubmission.TeamId))]
    public List<ChallengeSubmission> Submissions { get; set; } = null!;
}
