namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.BatchEmail;

public class Response
{
    public int TotalEmailsSent { get; set; }
    public int AcceptedEmailsSent { get; set; }
    public int RejectedEmailsSent { get; set; }
    public List<string> Errors { get; set; } = [];
}
