namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Resources.List;

public class Response
{
    public IEnumerable<ResourceItem> Resources { get; set; } = [];

    public class ResourceItem
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
    }
}
