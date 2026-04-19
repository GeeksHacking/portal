using FastEndpoints;
using FluentValidation;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Submissions.Create;

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Title).NotEmpty();

        RuleFor(x => x.RepoUri)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(IsAbsoluteHttpUri);

        RuleFor(x => x.DemoUri)
            .Must(IsAbsoluteHttpUri)
            .When(x => x.DemoUri is not null);

        RuleFor(x => x.SlidesUri)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(IsAbsoluteHttpUri);

        RuleFor(x => x.DevpostUri)
            .Must(IsAbsoluteHttpUri)
            .When(x => x.DevpostUri is not null);
    }

    private static bool IsAbsoluteHttpUri(Uri? uri)
    {
        return uri is not null
            && uri.IsAbsoluteUri
            && uri.Scheme is "http" or "https";
    }
}
