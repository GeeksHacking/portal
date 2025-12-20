namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Judges.Get;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid JudgeId { get; set; }
}
