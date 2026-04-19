namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.JoinByShortCode;

public class Response
{
    public Guid HackathonId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}
