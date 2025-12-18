namespace HackOMania.Api.Endpoints.Participants.Hackathon.Resources.Redeem;

public class Response
{
    public Guid RedemptionId { get; set; }
    public Guid ResourceId { get; set; }
    public Guid HackathonId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
