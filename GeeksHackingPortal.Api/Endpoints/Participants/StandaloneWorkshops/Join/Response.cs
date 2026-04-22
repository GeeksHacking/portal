namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Join;

public class Response
{
    public required Guid StandaloneWorkshopId { get; set; }
    public required Guid UserId { get; set; }
    public required DateTimeOffset JoinedAt { get; set; }
}
