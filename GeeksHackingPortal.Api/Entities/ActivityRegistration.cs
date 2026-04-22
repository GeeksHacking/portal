using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

[SugarIndex(
    "IX_ActivityRegistration_ActivityId_UserId",
    nameof(ActivityId),
    OrderByType.Asc,
    nameof(UserId),
    OrderByType.Asc
)]
public class ActivityRegistration
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid ActivityId { get; set; }

    public Guid UserId { get; set; }

    public ActivityRegistrationStatus Status { get; set; } = ActivityRegistrationStatus.Registered;

    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;

    [SugarColumn(IsNullable = true)]
    public DateTimeOffset? WithdrawnAt { get; set; }

    [Navigate(NavigateType.ManyToOne, nameof(ActivityId))]
    public Activity Activity { get; set; } = null!;

    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public User User { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ParticipantRegistrationSubmission.ActivityRegistrationId))]
    public List<ParticipantRegistrationSubmission> RegistrationSubmissions { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(VenueCheckIn.ActivityRegistrationId))]
    public List<VenueCheckIn> VenueCheckIns { get; set; } = null!;
}

public enum ActivityRegistrationStatus
{
    Registered = 1,
    Withdrawn = 2,
}
