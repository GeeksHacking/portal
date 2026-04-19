namespace GeeksHackingPortal.Tests.Models;

public class HackathonJoinResponse
{
    public Guid HackathonId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}

public class ParticipantStatusResponse
{
    public bool IsParticipant { get; set; }
    public bool IsOrganizer { get; set; }
    public Guid? TeamId { get; set; }
    public string? TeamName { get; set; }
    public string? Status { get; set; }
}
