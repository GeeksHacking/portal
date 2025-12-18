namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Judges.List;

public class Response
{
    public required List<JudgeItem> Judges { get; init; }
}

public class JudgeItem
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Guid Secret { get; init; }
    public required bool Active { get; init; }
}
