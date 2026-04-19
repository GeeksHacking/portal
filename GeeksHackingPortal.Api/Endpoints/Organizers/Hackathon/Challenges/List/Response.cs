namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Challenges.List;

public class Response
{
    public IEnumerable<ResponseChallenge> Challenges { get; set; } = [];

    public class ResponseChallenge
    {
        public required Guid Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Sponsor { get; set; }
        public required string Criteria { get; set; }
        public required bool IsPublished { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public required DateTimeOffset UpdatedAt { get; set; }
    }
}
