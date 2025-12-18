namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Judges.Get;

public class Request
{
    public string Id { get; set; } = null!;
    public Guid JudgeId { get; set; }
}
