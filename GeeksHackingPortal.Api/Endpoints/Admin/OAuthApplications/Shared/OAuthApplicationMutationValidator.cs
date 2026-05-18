using FastEndpoints;
using FluentValidation;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;

public class OAuthApplicationMutationValidator<TRequest> : Validator<TRequest>
    where TRequest : OAuthApplicationMutationRequest
{
    public OAuthApplicationMutationValidator()
    {
        RuleFor(x => x.ClientId)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .MinimumLength(3)
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9._:-]+$");

        RuleFor(x => x.DisplayName)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .MaximumLength(200);

        RuleFor(x => x.Platform).IsInEnum();

        RuleFor(x => x.RedirectUris).NotEmpty();
        RuleForEach(x => x.RedirectUris)
            .Must(uri => uri.IsAbsoluteUri)
            .WithMessage("Redirect URI must be absolute.");

        RuleForEach(x => x.PostLogoutRedirectUris)
            .Must(uri => uri.IsAbsoluteUri)
            .WithMessage("Post logout redirect URI must be absolute.");

        RuleForEach(x => x.RedirectUris)
            .Must((request, uri) => IsAllowedForPlatform(request.Platform, uri))
            .WithMessage("Web redirect URIs must use http or https.");

        RuleForEach(x => x.PostLogoutRedirectUris)
            .Must((request, uri) => IsAllowedForPlatform(request.Platform, uri))
            .WithMessage("Web post logout redirect URIs must use http or https.");
    }

    private static bool IsAllowedForPlatform(OAuthApplicationPlatform platform, Uri uri) =>
        platform is not OAuthApplicationPlatform.Web || uri.Scheme is "http" or "https";
}
