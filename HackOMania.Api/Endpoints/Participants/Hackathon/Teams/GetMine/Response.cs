namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.GetMine;

public class Response
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string JoinCode { get; set; } = null!;
    public IEnumerable<Response_Member> Members { get; set; } = [];

    public class Response_Member
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsCurrentUser { get; set; }
    }
}
