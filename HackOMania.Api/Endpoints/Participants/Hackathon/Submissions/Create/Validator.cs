using FastEndpoints;
using FluentValidation;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Submissions.Create;

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Title).Must(value => !string.IsNullOrWhiteSpace(value));

        RuleFor(x => x.RepoUri)
            .NotNull()
            .Must(uri => uri!.IsAbsoluteUri)
            .Must(uri => uri!.Scheme is "http" or "https");

        RuleFor(x => x.DemoUri)
            .Must(uri => uri!.IsAbsoluteUri)
            .Must(uri => uri!.Scheme is "http" or "https")
            .When(x => x.DemoUri is not null);

        RuleFor(x => x.SlidesUri)
            .NotNull()
            .Must(uri => uri!.IsAbsoluteUri)
            .Must(uri => uri!.Scheme is "http" or "https");

        RuleFor(x => x.DevpostUri)
            .Must(uri => uri!.IsAbsoluteUri)
            .Must(uri => uri!.Scheme is "http" or "https")
            .When(x => x.DevpostUri is not null);
    }
}
