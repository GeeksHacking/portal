namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.List;

public class Response
{
    public IEnumerable<Response_Challenge> Challenges { get; set; } = [];

    public class Response_Challenge
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? Criteria { get; set; }
        public bool IsPublished { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
