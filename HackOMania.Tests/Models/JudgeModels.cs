namespace HackOMania.Tests.Models;

public class JudgeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid Secret { get; set; }
    public bool Active { get; set; }
}

public class JudgesListResponse
{
    public IEnumerable<JudgeItem>? Judges { get; set; }
}

public class JudgeItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid Secret { get; set; }
    public bool Active { get; set; }
}
