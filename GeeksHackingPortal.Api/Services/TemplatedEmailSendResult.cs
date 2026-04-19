namespace GeeksHackingPortal.Api.Services;

public record TemplatedEmailSendResult(
    string ToEmail,
    string? TemplateId,
    string Provider,
    TemplatedEmailSendResult.SendStatus Status,
    DateTimeOffset SentAt,
    string? ProviderMessageId = null,
    string? ErrorMessage = null,
    string? CorrelationId = null
)
{
    public enum SendStatus
    {
        Sent = 1,
        Failed = 2,
        Skipped = 3,
    }
}
