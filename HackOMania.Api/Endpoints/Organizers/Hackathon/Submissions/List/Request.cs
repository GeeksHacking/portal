using FastEndpoints;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Submissions.List;

public class Request
{
    public Guid HackathonId { get; set; }

    [QueryParam]
    public Guid? ChallengeId { get; set; }

    [QueryParam]
    public Guid? TeamId { get; set; }
}
