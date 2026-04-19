using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public class VenueCheckIn
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid ParticipantId { get; set; }

    public Guid HackathonId { get; set; }

    public DateTimeOffset CheckInTime { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTimeOffset? CheckOutTime { get; set; }

    public bool IsCheckedIn { get; set; } = true;
}
