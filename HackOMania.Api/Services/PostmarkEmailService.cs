using HackOMania.Api.Options;
using Microsoft.Extensions.Options;
using PostmarkDotNet;

namespace HackOMania.Api.Services;

public class PostmarkEmailService : IEmailService
{
    private readonly PostmarkClient _client;
    private readonly PostmarkOptions _options;
    private readonly ILogger<PostmarkEmailService> _logger;

    public PostmarkEmailService(
        IOptions<PostmarkOptions> options,
        ILogger<PostmarkEmailService> logger
    )
    {
        _options = options.Value;
        _logger = logger;
        _client = new PostmarkClient(_options.ServerToken);
    }

    public async Task SendParticipantAcceptedEmailAsync(
        string toEmail,
        string toName,
        string hackathonName,
        string? reason = null,
        CancellationToken ct = default
    )
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Email sending is disabled. Skipping email to {Email}",
                toEmail
            );
            return;
        }

        try
        {
            var message = new TemplatedPostmarkMessage
            {
                From = $"{_options.FromName} <{_options.FromEmail}>",
                To = toEmail,
                TemplateId = long.TryParse(_options.AcceptedTemplateId, out var templateId) 
                    ? templateId 
                    : 0,
                TemplateAlias = !long.TryParse(_options.AcceptedTemplateId, out _) 
                    ? _options.AcceptedTemplateId 
                    : null,
                TemplateModel = new
                {
                    participant_name = toName,
                    hackathon_name = hackathonName,
                    reason = reason ?? string.Empty,
                    has_reason = !string.IsNullOrWhiteSpace(reason)
                }
            };

            var response = await _client.SendMessageAsync(message);

            if (response.Status != PostmarkStatus.Success)
            {
                _logger.LogError(
                    "Failed to send acceptance email to {Email}. Status: {Status}, Message: {Message}",
                    toEmail,
                    response.Status,
                    response.Message
                );
            }
            else
            {
                _logger.LogInformation(
                    "Successfully sent acceptance email to {Email}",
                    toEmail
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception occurred while sending acceptance email to {Email}",
                toEmail
            );
            // Don't throw - we don't want email failures to break the calling process
        }
    }

    public async Task SendParticipantRejectedEmailAsync(
        string toEmail,
        string toName,
        string hackathonName,
        string? reason = null,
        CancellationToken ct = default
    )
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Email sending is disabled. Skipping email to {Email}",
                toEmail
            );
            return;
        }

        try
        {
            var message = new TemplatedPostmarkMessage
            {
                From = $"{_options.FromName} <{_options.FromEmail}>",
                To = toEmail,
                TemplateId = long.TryParse(_options.RejectedTemplateId, out var templateId) 
                    ? templateId 
                    : 0,
                TemplateAlias = !long.TryParse(_options.RejectedTemplateId, out _) 
                    ? _options.RejectedTemplateId 
                    : null,
                TemplateModel = new
                {
                    participant_name = toName,
                    hackathon_name = hackathonName,
                    reason = reason ?? string.Empty,
                    has_reason = !string.IsNullOrWhiteSpace(reason)
                }
            };

            var response = await _client.SendMessageAsync(message);

            if (response.Status != PostmarkStatus.Success)
            {
                _logger.LogError(
                    "Failed to send rejection email to {Email}. Status: {Status}, Message: {Message}",
                    toEmail,
                    response.Status,
                    response.Message
                );
            }
            else
            {
                _logger.LogInformation(
                    "Successfully sent rejection email to {Email}",
                    toEmail
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception occurred while sending rejection email to {Email}",
                toEmail
            );
            // Don't throw - we don't want email failures to break the calling process
        }
    }

    public async Task SendBatchEmailsAsync(
        IEnumerable<(string Email, string Name, string Status, string? Reason)> participants,
        string hackathonName,
        CancellationToken ct = default
    )
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Email sending is disabled. Skipping batch emails.");
            return;
        }

        var tasks = participants.Select(async p =>
        {
            try
            {
                if (p.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
                {
                    await SendParticipantAcceptedEmailAsync(
                        p.Email,
                        p.Name,
                        hackathonName,
                        p.Reason,
                        ct
                    );
                }
                else if (p.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    await SendParticipantRejectedEmailAsync(
                        p.Email,
                        p.Name,
                        hackathonName,
                        p.Reason,
                        ct
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email to {Email} during batch operation",
                    p.Email
                );
                // Continue with other emails even if one fails
            }
        });

        await Task.WhenAll(tasks);
    }
}
