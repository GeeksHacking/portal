using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Data;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;
using GeeksHackingPortal.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Analytics;

public class Endpoint(
    IOpenIddictApplicationManager applicationManager,
    OpenIddictDbContext openIddictDbContext
) : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("admin/oauth-applications/analytics");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Get OAuth applications analytics";
            s.Description = "Gets analytics for OAuth applications owned by the current admin.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var ownerUserId =
            User.GetUserId() ?? throw new InvalidOperationException("The current user id is required.");

        var ownedAppIds = new List<string>();
        var appsInfo = new Dictionary<string, OAuthApplicationResponse>();

        await foreach (var application in applicationManager.ListAsync(cancellationToken: ct))
        {
            if (await OAuthApplicationMapper.IsOwnedByAsync(applicationManager, application, ownerUserId, ct))
            {
                var response = await OAuthApplicationMapper.ToResponseAsync(
                    applicationManager,
                    application,
                    clientSecret: null,
                    ct
                );
                ownedAppIds.Add(response.Id);
                appsInfo[response.Id] = response;
            }
        }

        var analytics = await openIddictDbContext
            .Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .Where(a => a.Application != null && ownedAppIds.Contains(a.Application.Id))
            .GroupBy(a => a.Application!.Id)
            .Select(g => new
            {
                ApplicationId = g.Key,
                TotalAuthorizations = g.Count(),
                UniqueUsers = g.Select(a => a.Subject).Distinct().Count()
            })
            .ToListAsync(ct);

        var items = appsInfo.Values.Select(app =>
        {
            var stat = analytics.FirstOrDefault(a => a.ApplicationId == app.Id);
            return new OAuthApplicationAnalyticsResponse
            {
                ApplicationId = app.Id,
                DisplayName = app.DisplayName,
                Platform = app.Platform,
                TotalAuthorizations = stat?.TotalAuthorizations ?? 0,
                UniqueUsers = stat?.UniqueUsers ?? 0
            };
        }).ToList();

        await Send.OkAsync(new Response { Items = items }, ct);
    }
}


