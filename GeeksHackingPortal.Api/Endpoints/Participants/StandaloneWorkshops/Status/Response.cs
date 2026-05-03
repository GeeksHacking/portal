namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Status;

public class Response
{
    public required bool IsRegistered { get; init; }
    public required bool IsOrganizer { get; init; }
    public Guid? RegistrationId { get; init; }
    public DateTimeOffset? RegisteredAt { get; init; }
    public DateTimeOffset? WithdrawnAt { get; init; }
}
