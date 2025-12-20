namespace HackOMania.Api.Endpoints.Participants.Hackathon.Submissions.List;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid TeamId { get; set; }
}
