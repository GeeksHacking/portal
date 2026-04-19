namespace GeeksHackingPortal.Api.Services;

public record TemplatedEmailRequest(
    string ToEmail,
    string? TemplateId,
    IReadOnlyDictionary<string, object> TemplateVariables,
    string? CorrelationId = null
);
