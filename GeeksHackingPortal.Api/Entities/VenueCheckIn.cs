using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public class VenueCheckIn
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [SugarColumn(OldColumnName = "ParticipantId")]
    public Guid ActivityRegistrationId { get; set; }

    [SugarColumn(OldColumnName = "HackathonId")]
    public Guid ActivityId { get; set; }

    public DateTimeOffset CheckInTime { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTimeOffset? CheckOutTime { get; set; }

    public bool IsCheckedIn { get; set; } = true;

    [Navigate(NavigateType.ManyToOne, nameof(ActivityRegistrationId))]
    public ActivityRegistration ActivityRegistration { get; set; } = null!;
}
