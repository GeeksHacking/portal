namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Judges.Create;

public class Response
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Guid Secret { get; init; }
    public required bool Active { get; init; }
}
