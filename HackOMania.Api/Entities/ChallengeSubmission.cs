using SqlSugar;

namespace HackOMania.Api.Entities;

public class ChallengeSubmission
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>
    /// Physical location of the team's booth/table
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string? Location { get; set; }

    /// <summary>
    /// Link to the Devpost submission
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public Uri? DevpostUri { get; set; }

    /// <summary>
    /// Link to the team's code repository
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public Uri? RepositoryUri { get; set; }

    /// <summary>
    /// Link to the demo (e.g., video or live demo)
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public Uri? DemoUri { get; set; }

    /// <summary>
    /// Link to the team's presentation slides
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public Uri? SlidesUri { get; set; }

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid HackathonId { get; set; }

    public Guid TeamId { get; set; }

    /// <summary>
    /// Challenge this submission is for (optional - general submission if null)
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public Guid? ChallengeId { get; set; }

    public Guid SubmittedByUserId { get; set; }
}
