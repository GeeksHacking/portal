using HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.List;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.Get;

public class Response : ParticipantItem
{
    public required List<ParticipantEmailDeliveryItem> EmailDeliveries { get; init; }
}

public class ParticipantEmailDeliveryItem
{
    public required Guid Id { get; init; }
    public required string EventKey { get; init; }
    public required string TemplateId { get; init; }
    public required string Provider { get; init; }
    public required string Status { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ProviderMessageId { get; init; }
    public required DateTimeOffset SentAt { get; init; }
}
