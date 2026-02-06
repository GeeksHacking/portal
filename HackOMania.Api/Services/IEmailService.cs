namespace HackOMania.Api.Services;

public interface IEmailService
{
    /// <summary>
    /// Send a single templated email.
    /// </summary>
    /// <param name="request">Templated email request</param>
    /// <param name="ct">Cancellation token</param>
    Task SendTemplatedEmailAsync(TemplatedEmailRequest request, CancellationToken ct = default);

    /// <summary>
    /// Send multiple templated emails.
    /// </summary>
    Task SendBatchTemplatedEmailsAsync(
        IEnumerable<TemplatedEmailRequest> emails,
        CancellationToken ct = default
    );
}
