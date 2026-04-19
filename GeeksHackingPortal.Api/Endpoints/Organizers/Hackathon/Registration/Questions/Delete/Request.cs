namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Registration.Questions.Delete;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid QuestionId { get; set; }
}
