namespace HackOMania.Tests.Models;

public class ResourceResponse
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? RedemptionStmt { get; set; }
}

public class ResourcesListResponse
{
    public IEnumerable<ResourceItem>? Resources { get; set; }
}

public class ResourceItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? RedemptionStmt { get; set; }
}

public class ParticipantResourceItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class ParticipantResourcesListResponse
{
    public IEnumerable<ParticipantResourceItem>? Resources { get; set; }
}

public class ResourceRedemptionResponse
{
    public Guid RedemptionId { get; set; }
    public Guid ResourceId { get; set; }
    public Guid HackathonId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
