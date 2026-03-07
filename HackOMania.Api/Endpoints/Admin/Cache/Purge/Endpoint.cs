using FastEndpoints;
using HackOMania.Api.Authorization;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Admin.Cache.Purge;

public class Endpoint(ICacheService cache, ILogger<Endpoint> logger) : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Post("admin/cache/purge");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Purge server cache";
            s.Description =
                "Clears the in-process SqlSugar data cache used by the API server.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var keys = cache.GetAllKey<object>().Distinct().ToList();

        foreach (var key in keys)
        {
            cache.Remove<object>(key);
        }

        var remainingKeys = cache.GetAllKey<object>().Count();
        var purgedAt = DateTimeOffset.UtcNow;

        logger.LogWarning(
            "Server cache purged by user {User}. Purged {PurgedKeys} keys, {RemainingKeys} remain.",
            User.Identity?.Name ?? "unknown",
            keys.Count,
            remainingKeys
        );

        await Send.OkAsync(
            new Response
            {
                Message = keys.Count == 0 ? "Cache was already empty." : "Cache purged successfully.",
                PurgedKeys = keys.Count,
                RemainingKeys = remainingKeys,
                PurgedAt = purgedAt,
            },
            ct
        );
    }
}
