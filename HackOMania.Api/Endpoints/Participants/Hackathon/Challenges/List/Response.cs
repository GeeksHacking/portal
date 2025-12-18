namespace HackOMania.Api.Endpoints.Participants.Hackathon.Challenges.List;

public class Response
{
    public IEnumerable<Response_Challenge> Challenges { get; set; } = [];

    public class Response_Challenge
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? Criteria { get; set; }
    }
}
