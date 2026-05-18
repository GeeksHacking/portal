using System.Security.Cryptography;
using System.Text.Json;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;

public static class OAuthApplicationMapper
{
    public const string OwnerUserIdProperty = "geeksHackingPortal:ownerUserId";

    public static OpenIddictApplicationDescriptor ToDescriptor(
        OAuthApplicationMutationRequest request,
        Guid ownerUserId,
        string? clientSecret
    )
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ApplicationType = request.Platform switch
            {
                OAuthApplicationPlatform.Web => ApplicationTypes.Web,
                OAuthApplicationPlatform.Native => ApplicationTypes.Native,
                _ => throw new ArgumentOutOfRangeException(nameof(request.Platform)),
            },
            ClientId = request.ClientId.Trim(),
            ClientSecret = request.Platform is OAuthApplicationPlatform.Web ? clientSecret : null,
            ClientType = request.Platform switch
            {
                OAuthApplicationPlatform.Web => ClientTypes.Confidential,
                OAuthApplicationPlatform.Native => ClientTypes.Public,
                _ => throw new ArgumentOutOfRangeException(nameof(request.Platform)),
            },
            ConsentType = ConsentTypes.Explicit,
            DisplayName = request.DisplayName.Trim(),
        };

        foreach (var uri in NormalizeUris(request.RedirectUris))
        {
            descriptor.RedirectUris.Add(uri);
        }

        foreach (var uri in NormalizeUris(request.PostLogoutRedirectUris))
        {
            descriptor.PostLogoutRedirectUris.Add(uri);
        }

        descriptor.Permissions.UnionWith(
            [
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
            ]
        );

        if (request.Platform is OAuthApplicationPlatform.Native)
        {
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        descriptor.Properties[OwnerUserIdProperty] = JsonSerializer.SerializeToElement(ownerUserId);

        return descriptor;
    }

    public static async Task<OAuthApplicationResponse> ToResponseAsync(
        IOpenIddictApplicationManager applicationManager,
        object application,
        string? clientSecret,
        CancellationToken ct
    )
    {
        return new OAuthApplicationResponse
        {
            Id = await applicationManager.GetIdAsync(application, ct) ?? string.Empty,
            ClientId = await applicationManager.GetClientIdAsync(application, ct) ?? string.Empty,
            ClientSecret = clientSecret,
            DisplayName = await applicationManager.GetDisplayNameAsync(application, ct) ?? string.Empty,
            Platform = await GetPlatformAsync(applicationManager, application, ct),
            RedirectUris = ToUris(await applicationManager.GetRedirectUrisAsync(application, ct)),
            PostLogoutRedirectUris = ToUris(
                await applicationManager.GetPostLogoutRedirectUrisAsync(application, ct)
            ),
        };
    }

    public static async ValueTask<bool> IsOwnedByAsync(
        IOpenIddictApplicationManager applicationManager,
        object application,
        Guid ownerUserId,
        CancellationToken ct
    )
    {
        var properties = await applicationManager.GetPropertiesAsync(application, ct);

        return properties.TryGetValue(OwnerUserIdProperty, out var owner)
            && owner.ValueKind is JsonValueKind.String
            && Guid.TryParse(owner.GetString(), out var storedOwnerUserId)
            && storedOwnerUserId == ownerUserId;
    }

    public static string GenerateClientSecret() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    public static IReadOnlyList<Uri> NormalizeUris(IEnumerable<Uri>? uris) =>
        (uris ?? [])
            .Where(uri => uri.IsAbsoluteUri)
            .Select(uri => new Uri(uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped)))
            .Distinct()
            .ToList();

    private static IReadOnlyList<Uri> ToUris(IEnumerable<string> uris) =>
        uris.Select(uri => new Uri(uri, UriKind.Absolute)).ToList();

    private static async ValueTask<OAuthApplicationPlatform> GetPlatformAsync(
        IOpenIddictApplicationManager applicationManager,
        object application,
        CancellationToken ct
    )
    {
        var applicationType = await applicationManager.GetApplicationTypeAsync(application, ct);

        return applicationType switch
        {
            ApplicationTypes.Native => OAuthApplicationPlatform.Native,
            _ => OAuthApplicationPlatform.Web,
        };
    }
}
