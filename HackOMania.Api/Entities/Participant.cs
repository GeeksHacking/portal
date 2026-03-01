using SqlSugar;

namespace HackOMania.Api.Entities;

public class Participant : HackathonUser
{
    [SugarColumn(IsNullable = true)]
    public Guid? TeamId { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    [SugarColumn(IsNullable = true)]
    public DateTimeOffset? LeftAt { get; set; }

    [Navigate(NavigateType.ManyToOne, nameof(TeamId), nameof(Team.Id))]
    public Team? Team { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ParticipantReview.ParticipantId))]
    public List<ParticipantReview> ParticipantReviews { get; set; } = [];

    [Navigate(NavigateType.OneToMany, nameof(ParticipantEmailDelivery.ParticipantId))]
    public List<ParticipantEmailDelivery> EmailDeliveries { get; set; } = [];

    /// <summary>
    /// Registration submissions for dynamic questions
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(ParticipantRegistrationSubmission.Id))]
    public List<ParticipantRegistrationSubmission> RegistrationSubmissions { get; set; } = null!;

    [SugarColumn(IsIgnore = true)]
    public ParticipantReview.ParticipantReviewStatus? ConcludedStatus =>
        ParticipantReviews?.OrderByDescending(x => x.CreatedAt).FirstOrDefault()?.Status;
}
