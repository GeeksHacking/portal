using FastEndpoints;
using FluentValidation;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Create;

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Title).Must(value => !string.IsNullOrWhiteSpace(value));
        RuleFor(x => x.Description).Must(value => !string.IsNullOrWhiteSpace(value));
        RuleFor(x => x.Location).Must(value => !string.IsNullOrWhiteSpace(value));

        RuleFor(x => x.HomepageUri)
            .NotNull()
            .Must(uri => uri.IsAbsoluteUri)
            .Must(uri => uri.Scheme is "http" or "https");

        RuleFor(x => x.ShortCode)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .MinimumLength(3)
            .MaximumLength(16)
            .Matches("^[A-Za-z0-9-]+$");

        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime);
        RuleFor(x => x.MaxParticipants).GreaterThan(0);

        RuleForEach(x => x.EmailTemplates)
            .Must(entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
            .WithMessage("Email template keys and values are required.");
    }
}
