namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Organizers.List;

public class Response
{
    public IEnumerable<Response_Organizer> Organizers { get; set; } = [];

    public class Response_Organizer
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Type { get; set; } = null!;
    }
}
