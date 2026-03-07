namespace HackOMania.Api.Endpoints.Admin.Cache.Purge;

public class Response
{
    public required string Message { get; init; }
    public required int PurgedKeys { get; init; }
    public required int RemainingKeys { get; init; }
    public required DateTimeOffset PurgedAt { get; init; }
}
