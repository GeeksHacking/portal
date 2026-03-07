namespace HackOMania.Tests.Models;

public class ResourceResponse
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? RedemptionStmt { get; set; }
    public bool IsPublished { get; set; }
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
    public bool IsPublished { get; set; }
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

public class OrganizerResourceOverviewResponse
{
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = "";
    public bool IsPublished { get; set; }
    public int TotalRedemptions { get; set; }
    public int UniqueRedeemers { get; set; }
    public IEnumerable<OrganizerResourceParticipantItem>? Participants { get; set; }
    public IEnumerable<OrganizerResourceAuditTrailItem>? AuditTrail { get; set; }
}

public class OrganizerResourceParticipantItem
{
    public Guid ParticipantId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public bool HasRedeemed { get; set; }
    public int RedemptionCount { get; set; }
    public DateTimeOffset? LastRedeemedAt { get; set; }
}

public class OrganizerResourceAuditTrailItem
{
    public Guid RedemptionId { get; set; }
    public Guid ParticipantId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
}

public class OrganizerResourceHistoryResponse
{
    public Guid ParticipantId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = "";
    public bool ResourceIsPublished { get; set; }
    public bool HasRedeemed { get; set; }
    public int RedemptionCount { get; set; }
    public IEnumerable<OrganizerResourceHistoryItem>? History { get; set; }
}

public class OrganizerResourceHistoryItem
{
    public Guid RedemptionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
