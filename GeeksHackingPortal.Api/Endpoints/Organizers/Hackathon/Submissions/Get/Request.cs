namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Submissions.Get;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid SubmissionId { get; set; }
}
