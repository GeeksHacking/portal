namespace HackOMania.Api.Services;

public interface IEmailService
{
    /// <summary>
    /// Send a participant acceptance email
    /// </summary>
    Task SendParticipantAcceptedEmailAsync(
        string toEmail,
        string toName,
        string hackathonName,
        string? templateId,
        string? reason = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Send a participant rejection email
    /// </summary>
    Task SendParticipantRejectedEmailAsync(
        string toEmail,
        string toName,
        string hackathonName,
        string? templateId,
        string? reason = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Send batch emails to multiple participants
    /// </summary>
    Task SendBatchEmailsAsync(
        IEnumerable<(string Email, string Name, string Status, string? Reason)> participants,
        string hackathonName,
        string? acceptedTemplateId,
        string? rejectedTemplateId,
        CancellationToken ct = default
    );
}
