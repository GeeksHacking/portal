using HackOMania.Api.Entities;

namespace HackOMania.Api.Services;

public class HookService : IHookService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<HookService> _logger;

    public HookService(IEmailService emailService, ILogger<HookService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task OnParticipantReviewedAsync(
        Participant participant,
        User user,
        Entities.Hackathon hackathon,
        ParticipantReview.ParticipantReviewStatus status,
        string? reason = null,
        CancellationToken ct = default
    )
    {
        try
        {
            _logger.LogInformation(
                "Triggering hook for participant {UserId} review status {Status} for hackathon {HackathonId}",
                user.Id,
                status,
                hackathon.Id
            );

            if (status == ParticipantReview.ParticipantReviewStatus.Accepted)
            {
                await _emailService.SendParticipantAcceptedEmailAsync(
                    user.Email,
                    user.Name,
                    hackathon.Name,
                    hackathon.AcceptedEmailTemplateId,
                    reason,
                    ct
                );
            }
            else if (status == ParticipantReview.ParticipantReviewStatus.Rejected)
            {
                await _emailService.SendParticipantRejectedEmailAsync(
                    user.Email,
                    user.Name,
                    hackathon.Name,
                    hackathon.RejectedEmailTemplateId,
                    reason,
                    ct
                );
            }

            _logger.LogInformation(
                "Successfully triggered hook for participant {UserId}",
                user.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error triggering hook for participant {UserId}",
                user.Id
            );
            // Don't throw - we don't want email failures to break the review process
        }
    }
}
