namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Workshops.Join;

public class Response
{
    public required Guid Id { get; set; }
    public required Guid WorkshopId { get; set; }
    public required string WorkshopTitle { get; set; }
    public required DateTimeOffset JoinedAt { get; set; }
}
