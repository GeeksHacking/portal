using HackOMania.Api.Entities;

namespace HackOMania.Api.Services;

public static class ParticipantReviewEmailTemplateModelFactory
{
    public static Dictionary<string, object> Create(
        Participant participant,
        User user,
        Hackathon hackathon,
        string reviewStatus,
        string? reason
    )
    {
        return new Dictionary<string, object>
        {
            ["participant_name"] = user.Name,
            ["participant_first_name"] = user.FirstName,
            ["participant_last_name"] = user.LastName,
            ["participant_email"] = user.Email,
            ["participant_id"] = participant.Id.ToString(),
            ["user_id"] = user.Id.ToString(),
            ["hackathon_name"] = hackathon.Name,
            ["hackathon_id"] = hackathon.Id.ToString(),
            ["hackathon_short_code"] = hackathon.ShortCode,
            ["hackathon_venue"] = hackathon.Venue,
            ["hackathon_description"] = hackathon.Description,
            ["hackathon_homepage_url"] = hackathon.HomepageUri.ToString(),
            ["event_start_date"] = hackathon.EventStartDate.ToString("yyyy-MM-dd"),
            ["event_end_date"] = hackathon.EventEndDate.ToString("yyyy-MM-dd"),
            ["event_start_date_formatted"] = hackathon.EventStartDate.ToString("MMMM dd, yyyy"),
            ["event_end_date_formatted"] = hackathon.EventEndDate.ToString("MMMM dd, yyyy"),
            ["submissions_start_date"] = hackathon.SubmissionsStartDate.ToString("yyyy-MM-dd"),
            ["submissions_end_date"] = hackathon.SubmissionsEndDate.ToString("yyyy-MM-dd"),
            ["submissions_start_date_formatted"] = hackathon.SubmissionsStartDate.ToString(
                "MMMM dd, yyyy"
            ),
            ["submissions_end_date_formatted"] = hackathon.SubmissionsEndDate.ToString(
                "MMMM dd, yyyy"
            ),
            ["reason"] = reason ?? string.Empty,
            ["has_reason"] = !string.IsNullOrWhiteSpace(reason),
            ["review_status"] = reviewStatus,
            ["joined_at"] = participant.JoinedAt.ToString("yyyy-MM-dd"),
            ["joined_at_formatted"] = participant.JoinedAt.ToString("MMMM dd, yyyy"),
        };
    }
}
