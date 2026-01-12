namespace HackOMania.Tests.Models;

public class TeamResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? JoinCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class TeamsListResponse
{
    public IEnumerable<TeamItem>? Teams { get; set; }

    public class TeamItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
        public int MemberCount { get; set; }
    }
}
