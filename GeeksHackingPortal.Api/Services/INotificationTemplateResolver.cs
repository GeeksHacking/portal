namespace GeeksHackingPortal.Api.Services;

public interface INotificationTemplateResolver
{
    Task<string?> ResolveTemplateIdAsync(
        Guid hackathonId,
        string eventKey,
        CancellationToken ct = default
    );

    Task<Dictionary<string, string>> GetHackathonTemplatesAsync(
        Guid hackathonId,
        CancellationToken ct = default
    );

    Task<Dictionary<Guid, Dictionary<string, string>>> GetHackathonTemplatesAsync(
        IEnumerable<Guid> hackathonIds,
        CancellationToken ct = default
    );
}
