using HackOMania.Api.Options;
using Microsoft.Extensions.Options;
using PostmarkDotNet;

namespace HackOMania.Api.Services;

public class PostmarkEmailService : IEmailService
{
    private readonly PostmarkClient? _client;
    private readonly PostmarkOptions _options;
    private readonly ILogger<PostmarkEmailService> _logger;

    public PostmarkEmailService(
        IOptions<PostmarkOptions> options,
        ILogger<PostmarkEmailService> logger
    )
    {
        _options = options.Value;
        _logger = logger;
        _client = string.IsNullOrWhiteSpace(_options.ServerToken)
            ? null
            : new PostmarkClient(_options.ServerToken);
    }

    public async Task SendTemplatedEmailAsync(
        TemplatedEmailRequest request,
        CancellationToken ct = default
    )
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Email sending is disabled. Skipping email to {Email}",
                request.ToEmail
            );
            return;
        }

        if (_client is null)
        {
            _logger.LogWarning(
                "Postmark server token is missing. Skipping email to {Email}",
                request.ToEmail
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            _logger.LogWarning(
                "No email template configured. Skipping email to {Email}",
                request.ToEmail
            );
            return;
        }

        try
        {
            var templateId = request.TemplateId.Trim();
            var hasNumericTemplateId = long.TryParse(templateId, out var templateIdNum);
            var parsedTemplateId = hasNumericTemplateId ? templateIdNum : 0;
            var templateAlias = hasNumericTemplateId ? null : templateId;

            var message = new TemplatedPostmarkMessage
            {
                From = $"{_options.FromName} <{_options.FromEmail}>",
                To = request.ToEmail,
                TemplateId = parsedTemplateId,
                TemplateAlias = templateAlias,
                TemplateModel = request.TemplateVariables,
            };

            var response = await _client.SendMessageAsync(message);

            if (response.Status != PostmarkStatus.Success)
            {
                _logger.LogError(
                    "Failed to send email to {Email}. Status: {Status}, Message: {Message}",
                    request.ToEmail,
                    response.Status,
                    response.Message
                );
            }
            else
            {
                _logger.LogInformation("Successfully sent email to {Email}", request.ToEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception occurred while sending email to {Email}",
                request.ToEmail
            );
            // Don't throw - we don't want email failures to break the calling process
        }
    }

    public async Task SendBatchTemplatedEmailsAsync(
        IEnumerable<TemplatedEmailRequest> emails,
        CancellationToken ct = default
    )
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Email sending is disabled. Skipping batch emails.");
            return;
        }

        if (_client is null)
        {
            _logger.LogWarning("Postmark server token is missing. Skipping batch emails.");
            return;
        }

        await Parallel.ForEachAsync(
            emails,
            new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 8 },
            async (email, token) =>
            {
                try
                {
                    await SendTemplatedEmailAsync(email, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send email to {Email} during batch operation",
                        email.ToEmail
                    );
                    // Continue with other emails even if one fails
                }
            }
        );
    }
}
