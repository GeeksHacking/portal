using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public class PreviewSessionHandoff
{
    [SugarColumn(IsPrimaryKey = true, Length = 64)]
    public string NonceHash { get; set; } = null!;

    public string Origin { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }
}
