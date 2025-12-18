namespace HackOMania.Api.Endpoints.Participants.Hackathon.Resources.List;

public class Response
{
    public IEnumerable<Response_Resource> Resources { get; set; } = [];

    public class Response_Resource
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
