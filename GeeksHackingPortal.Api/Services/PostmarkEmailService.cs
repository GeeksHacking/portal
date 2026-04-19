using GeeksHackingPortal.Api.Options;
using Microsoft.Extensions.Options;
using PostmarkDotNet;

namespace GeeksHackingPortal.Api.Services;

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

    public async Task<TemplatedEmailSendResult> SendTemplatedEmailAsync(
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
            return new TemplatedEmailSendResult(
                request.ToEmail,
                request.TemplateId,
                "postmark",
                TemplatedEmailSendResult.SendStatus.Skipped,
                DateTimeOffset.UtcNow,
                ErrorMessage: "Email sending is disabled",
                CorrelationId: request.CorrelationId
            );
        }

        if (_client is null)
        {
            _logger.LogWarning(
                "Postmark server token is missing. Skipping email to {Email}",
                request.ToEmail
            );
            return new TemplatedEmailSendResult(
                request.ToEmail,
                request.TemplateId,
                "postmark",
                TemplatedEmailSendResult.SendStatus.Skipped,
                DateTimeOffset.UtcNow,
                ErrorMessage: "Postmark server token is missing",
                CorrelationId: request.CorrelationId
            );
        }

        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            _logger.LogWarning(
                "No email template configured. Skipping email to {Email}",
                request.ToEmail
            );
            return new TemplatedEmailSendResult(
                request.ToEmail,
                request.TemplateId,
                "postmark",
                TemplatedEmailSendResult.SendStatus.Skipped,
                DateTimeOffset.UtcNow,
                ErrorMessage: "No template configured",
                CorrelationId: request.CorrelationId
            );
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
                return new TemplatedEmailSendResult(
                    request.ToEmail,
                    request.TemplateId,
                    "postmark",
                    TemplatedEmailSendResult.SendStatus.Failed,
                    DateTimeOffset.UtcNow,
                    ErrorMessage: response.Message,
                    CorrelationId: request.CorrelationId
                );
            }

            _logger.LogInformation("Successfully sent email to {Email}", request.ToEmail);
            return new TemplatedEmailSendResult(
                request.ToEmail,
                request.TemplateId,
                "postmark",
                TemplatedEmailSendResult.SendStatus.Sent,
                DateTimeOffset.UtcNow,
                ProviderMessageId: response.MessageID.ToString(),
                CorrelationId: request.CorrelationId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception occurred while sending email to {Email}",
                request.ToEmail
            );
            // Don't throw - we don't want email failures to break the calling process
            return new TemplatedEmailSendResult(
                request.ToEmail,
                request.TemplateId,
                "postmark",
                TemplatedEmailSendResult.SendStatus.Failed,
                DateTimeOffset.UtcNow,
                ErrorMessage: ex.Message,
                CorrelationId: request.CorrelationId
            );
        }
    }

    public async Task<IReadOnlyList<TemplatedEmailSendResult>> SendBatchTemplatedEmailsAsync(
        IEnumerable<TemplatedEmailRequest> emails,
        CancellationToken ct = default
    )
    {
        var emailList = emails as IList<TemplatedEmailRequest> ?? emails.ToList();

        if (!_options.Enabled)
        {
            _logger.LogInformation("Email sending is disabled. Skipping batch emails.");
            return emailList
                .Select(e => new TemplatedEmailSendResult(
                    e.ToEmail,
                    e.TemplateId,
                    "postmark",
                    TemplatedEmailSendResult.SendStatus.Skipped,
                    DateTimeOffset.UtcNow,
                    ErrorMessage: "Email sending is disabled",
                    CorrelationId: e.CorrelationId
                ))
                .ToList();
        }

        if (_client is null)
        {
            _logger.LogWarning("Postmark server token is missing. Skipping batch emails.");
            return emailList
                .Select(e => new TemplatedEmailSendResult(
                    e.ToEmail,
                    e.TemplateId,
                    "postmark",
                    TemplatedEmailSendResult.SendStatus.Skipped,
                    DateTimeOffset.UtcNow,
                    ErrorMessage: "Postmark server token is missing",
                    CorrelationId: e.CorrelationId
                ))
                .ToList();
        }

        var results = new List<TemplatedEmailSendResult>();
        var gate = new object();

        await Parallel.ForEachAsync(
            emailList,
            new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 8 },
            async (email, token) =>
            {
                try
                {
                    var result = await SendTemplatedEmailAsync(email, token);
                    lock (gate)
                    {
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send email to {Email} during batch operation",
                        email.ToEmail
                    );
                    // Continue with other emails even if one fails
                    lock (gate)
                    {
                        results.Add(
                            new TemplatedEmailSendResult(
                                email.ToEmail,
                                email.TemplateId,
                                "postmark",
                                TemplatedEmailSendResult.SendStatus.Failed,
                                DateTimeOffset.UtcNow,
                                ErrorMessage: ex.Message,
                                CorrelationId: email.CorrelationId
                            )
                        );
                    }
                }
            }
        );

        return results;
    }
}
