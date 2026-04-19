using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public class Judge
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid Secret { get; set; } = Guid.NewGuid();

    public bool Active { get; set; }

    public Guid HackathonId { get; set; }
}
