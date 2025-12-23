using SqlSugar;

namespace HackOMania.Api.Entities;

public class Participant : HackathonUser
{
    [SugarColumn(IsNullable = true)]
    public Guid? TeamId { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    [Navigate(NavigateType.ManyToOne, nameof(TeamId), nameof(Team.Id))]
    public Team Team { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ParticipantReview.ParticipantId))]
    public List<ParticipantReview> ParticipantReviews { get; set; } = null!;

    public ParticipantReview.ParticipantReviewStatus? ConcludedStatus =>
        ParticipantReviews.OrderByDescending(x => x.CreatedAt).FirstOrDefault()?.Status;
}
