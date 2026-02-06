namespace HackOMania.Api.Services;

public interface IEmailService
{
    /// <summary>
    /// Send a participant acceptance email
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="templateId">Postmark template ID or alias</param>
    /// <param name="templateVariables">Dictionary of template variables to pass to the email template</param>
    /// <param name="ct">Cancellation token</param>
    Task SendParticipantAcceptedEmailAsync(
        string toEmail,
        string? templateId,
        Dictionary<string, object> templateVariables,
        CancellationToken ct = default
    );

    /// <summary>
    /// Send a participant rejection email
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="templateId">Postmark template ID or alias</param>
    /// <param name="templateVariables">Dictionary of template variables to pass to the email template</param>
    /// <param name="ct">Cancellation token</param>
    Task SendParticipantRejectedEmailAsync(
        string toEmail,
        string? templateId,
        Dictionary<string, object> templateVariables,
        CancellationToken ct = default
    );

    /// <summary>
    /// Send batch emails to multiple participants
    /// </summary>
    Task SendBatchEmailsAsync(
        IEnumerable<(string Email, string Status, Dictionary<string, object> TemplateVariables)> participants,
        string? acceptedTemplateId,
        string? rejectedTemplateId,
        CancellationToken ct = default
    );
}
