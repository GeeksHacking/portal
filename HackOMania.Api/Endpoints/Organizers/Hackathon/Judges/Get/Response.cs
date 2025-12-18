namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Judges.Get;

public class Response
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Guid Secret { get; init; }
    public required bool Active { get; init; }
}
