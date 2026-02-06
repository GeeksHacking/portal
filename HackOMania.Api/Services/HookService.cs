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

            // Build comprehensive template variables with all available context
            var templateVariables = new Dictionary<string, object>
            {
                // User/Participant information
                ["participant_name"] = user.Name,
                ["participant_first_name"] = user.FirstName,
                ["participant_last_name"] = user.LastName,
                ["participant_email"] = user.Email,
                ["participant_id"] = participant.Id.ToString(),
                ["user_id"] = user.Id.ToString(),
                
                // Hackathon information
                ["hackathon_name"] = hackathon.Name,
                ["hackathon_id"] = hackathon.Id.ToString(),
                ["hackathon_short_code"] = hackathon.ShortCode,
                ["hackathon_venue"] = hackathon.Venue,
                ["hackathon_description"] = hackathon.Description,
                ["hackathon_homepage_url"] = hackathon.HomepageUri.ToString(),
                
                // Event dates
                ["event_start_date"] = hackathon.EventStartDate.ToString("yyyy-MM-dd"),
                ["event_end_date"] = hackathon.EventEndDate.ToString("yyyy-MM-dd"),
                ["event_start_date_formatted"] = hackathon.EventStartDate.ToString("MMMM dd, yyyy"),
                ["event_end_date_formatted"] = hackathon.EventEndDate.ToString("MMMM dd, yyyy"),
                
                // Submissions dates
                ["submissions_start_date"] = hackathon.SubmissionsStartDate.ToString("yyyy-MM-dd"),
                ["submissions_end_date"] = hackathon.SubmissionsEndDate.ToString("yyyy-MM-dd"),
                ["submissions_start_date_formatted"] = hackathon.SubmissionsStartDate.ToString("MMMM dd, yyyy"),
                ["submissions_end_date_formatted"] = hackathon.SubmissionsEndDate.ToString("MMMM dd, yyyy"),
                
                // Review information
                ["reason"] = reason ?? string.Empty,
                ["has_reason"] = !string.IsNullOrWhiteSpace(reason),
                ["review_status"] = status.ToString(),
                
                // Participant metadata
                ["joined_at"] = participant.JoinedAt.ToString("yyyy-MM-dd"),
                ["joined_at_formatted"] = participant.JoinedAt.ToString("MMMM dd, yyyy"),
            };

            if (status == ParticipantReview.ParticipantReviewStatus.Accepted)
            {
                await _emailService.SendParticipantAcceptedEmailAsync(
                    user.Email,
                    hackathon.AcceptedEmailTemplateId,
                    templateVariables,
                    ct
                );
            }
            else if (status == ParticipantReview.ParticipantReviewStatus.Rejected)
            {
                await _emailService.SendParticipantRejectedEmailAsync(
                    user.Email,
                    hackathon.RejectedEmailTemplateId,
                    templateVariables,
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
