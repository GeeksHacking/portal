namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Judges.Update;

public class Request
{
    public string Id { get; set; } = null!;
    public Guid JudgeId { get; set; }
    public string? Name { get; set; }
    public bool? Active { get; set; }

    /// <summary>
    /// If true, regenerates the judge's secret
    /// </summary>
    public bool RegenerateSecret { get; set; }
}
