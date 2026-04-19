using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public class WorkshopParticipant
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid WorkshopId { get; set; }

    public Guid ParticipantId { get; set; }

    public Guid HackathonId { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool HasAttended { get; set; } = false;

    [SugarColumn(IsNullable = true)]
    public DateTimeOffset? AttendedAt { get; set; }

    [Navigate(NavigateType.ManyToOne, nameof(WorkshopId), nameof(Workshop.Id))]
    public Workshop Workshop { get; set; } = null!;
}
