namespace HackOMania.Api.Endpoints.Participants.Hackathon.Join;

public class Response
{
    public Guid HackathonId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}
