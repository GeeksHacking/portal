using FastEndpoints;
using FluentValidation;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.StandaloneWorkshops;

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.ActivityId).NotEmpty();

        When(x => x.HomepageUri is not null, () =>
        {
            RuleFor(x => x.HomepageUri!)
                .Must(uri => uri.IsAbsoluteUri)
                .Must(uri => uri.Scheme is "http" or "https");
        });

        When(x => x.ShortCode is not null, () =>
        {
            RuleFor(x => x.ShortCode!)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                .MinimumLength(3)
                .MaximumLength(16)
                .Matches("^[A-Za-z0-9-]+$");
        });

        When(x => x.MaxParticipants.HasValue, () =>
        {
            RuleFor(x => x.MaxParticipants!.Value).GreaterThan(0);
        });
    }
}
