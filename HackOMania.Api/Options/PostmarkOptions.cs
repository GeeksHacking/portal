namespace HackOMania.Api.Options;

public class PostmarkOptions
{
    /// <summary>
    /// Postmark API Server Token
    /// </summary>
    public string ServerToken { get; set; } = string.Empty;

    /// <summary>
    /// Default sender email address
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Default sender name
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Enable or disable email sending (useful for testing)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Postmark template ID or alias for participant acceptance emails
    /// </summary>
    public string AcceptedTemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Postmark template ID or alias for participant rejection emails
    /// </summary>
    public string RejectedTemplateId { get; set; } = string.Empty;
}
