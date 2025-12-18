using SqlSugar;

namespace HackOMania.Api.Entities;

public class Challenge
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid HackathonId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string SelectionCriteriaStmt { get; set; } = "true";

    public bool IsPublished { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
