using FastEndpoints;
using FluentValidation;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Update;

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.StandaloneWorkshopId)
            .NotEmpty()
            .WithMessage("Standalone workshop id is required.");

        When(x => x.Title is not null, () =>
        {
            RuleFor(x => x.Title!)
                .Cascade(CascadeMode.Stop)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                .WithMessage("Title is required.")
                .MaximumLength(160)
                .WithMessage("Title must be 160 characters or fewer.");
        });

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .Cascade(CascadeMode.Stop)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                .WithMessage("Description is required.")
                .MaximumLength(4000)
                .WithMessage("Description must be 4000 characters or fewer.");
        });

        When(x => x.Location is not null, () =>
        {
            RuleFor(x => x.Location!)
                .Cascade(CascadeMode.Stop)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                .WithMessage("Location is required.")
                .MaximumLength(240)
                .WithMessage("Location must be 240 characters or fewer.");
        });

        When(x => x.HomepageUri is not null, () =>
        {
            RuleFor(x => x.HomepageUri!)
                .Cascade(CascadeMode.Stop)
                .Must(uri => uri.IsAbsoluteUri)
                .WithMessage("Homepage URL must be an absolute URL.")
                .Must(uri => uri.Scheme is "http" or "https")
                .WithMessage("Homepage URL must start with http:// or https://.");
        });

        When(x => x.ShortCode is not null, () =>
        {
            RuleFor(x => x.ShortCode!)
                .Cascade(CascadeMode.Stop)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                .WithMessage("Short code is required.")
                .MinimumLength(3)
                .WithMessage("Short code must be at least 3 characters.")
                .MaximumLength(16)
                .WithMessage("Short code must be 16 characters or fewer.")
                .Matches("^[A-Za-z0-9-]+$")
                .WithMessage("Short code can only contain letters, numbers, and hyphens.");
        });

        When(x => x.StartTime.HasValue && x.EndTime.HasValue, () =>
        {
            RuleFor(x => x.EndTime!.Value)
                .GreaterThan(x => x.StartTime!.Value)
                .WithMessage("End time must be after start time.");
        });

        When(x => x.MaxParticipants.HasValue, () =>
        {
            RuleFor(x => x.MaxParticipants!.Value)
                .GreaterThan(0)
                .WithMessage("Max participants must be greater than 0.");
        });

        RuleForEach(x => x.EmailTemplates)
            .Must(entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
            .WithMessage("Email template keys and values are required.");
    }
}
