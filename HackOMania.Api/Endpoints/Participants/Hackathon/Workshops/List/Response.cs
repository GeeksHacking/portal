namespace HackOMania.Api.Endpoints.Participants.Hackathon.Workshops.List;

public class Response
{
    public required List<WorkshopDto> Workshops { get; set; }
}

public class WorkshopDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public required string Location { get; set; }
    public required int MaxParticipants { get; set; }
    public required int CurrentParticipants { get; set; }
    public required bool IsJoined { get; set; }
    public required bool HasAttended { get; set; }
}
