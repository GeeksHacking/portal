using FastEndpoints;
using FluentValidation;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Create;

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Title is required.")
            .MaximumLength(160)
            .WithMessage("Title must be 160 characters or fewer.");

        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Description is required.")
            .MaximumLength(4000)
            .WithMessage("Description must be 4000 characters or fewer.");

        RuleFor(x => x.Location)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Location is required.")
            .MaximumLength(240)
            .WithMessage("Location must be 240 characters or fewer.");

        RuleFor(x => x.HomepageUri)
            .NotNull()
            .WithMessage("Homepage URL is required.");

        When(x => x.HomepageUri is not null, () =>
        {
            RuleFor(x => x.HomepageUri!)
                .Cascade(CascadeMode.Stop)
                .Must(uri => uri.IsAbsoluteUri)
                .WithMessage("Homepage URL must be an absolute URL.")
                .Must(uri => uri.Scheme is "http" or "https")
                .WithMessage("Homepage URL must start with http:// or https://.");
        });

        RuleFor(x => x.ShortCode)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Short code is required.")
            .MinimumLength(3)
            .WithMessage("Short code must be at least 3 characters.")
            .MaximumLength(16)
            .WithMessage("Short code must be 16 characters or fewer.")
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("Short code can only contain letters, numbers, and hyphens.");

        RuleFor(x => x.StartTime)
            .GreaterThan(DateTimeOffset.MinValue)
            .WithMessage("Start time is required.");

        RuleFor(x => x.EndTime)
            .GreaterThan(DateTimeOffset.MinValue)
            .WithMessage("End time is required.")
            .GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(0)
            .WithMessage("Max participants must be greater than 0.");

        RuleForEach(x => x.EmailTemplates)
            .Must(entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
            .WithMessage("Email template keys and values are required.");
    }
}
