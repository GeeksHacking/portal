using FastEndpoints;
using FluentValidation;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Create;

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Name).Must(value => !string.IsNullOrWhiteSpace(value));
        RuleFor(x => x.Description).Must(value => !string.IsNullOrWhiteSpace(value));
        RuleFor(x => x.Venue).Must(value => !string.IsNullOrWhiteSpace(value));

        RuleFor(x => x.HomepageUri)
            .NotNull()
            .Must(uri => uri.IsAbsoluteUri)
            .Must(uri => uri.Scheme is "http" or "https");

        RuleFor(x => x.ShortCode)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .MinimumLength(3)
            .MaximumLength(16)
            .Matches("^[A-Za-z0-9-]+$");

        RuleFor(x => x.EventEndDate).GreaterThan(x => x.EventStartDate);

        RuleFor(x => x.SubmissionsStartDate).GreaterThanOrEqualTo(x => x.EventStartDate);
        RuleFor(x => x.SubmissionsEndDate).GreaterThanOrEqualTo(x => x.SubmissionsStartDate);
        RuleFor(x => x.SubmissionsEndDate).LessThanOrEqualTo(x => x.EventEndDate);
        RuleFor(x => x.ChallengeSelectionEndDate)
            .Must((request, value) => (value ?? request.SubmissionsEndDate) >= request.SubmissionsStartDate)
            .WithMessage("'Challenge Selection End Date' must be greater than or equal to 'Submissions Start Date'.");
        RuleFor(x => x.ChallengeSelectionEndDate)
            .Must((request, value) => (value ?? request.SubmissionsEndDate) <= request.SubmissionsEndDate)
            .WithMessage("'Challenge Selection End Date' must be less than or equal to 'Submissions End Date'.");

        RuleFor(x => x.JudgingStartDate).GreaterThanOrEqualTo(x => x.SubmissionsEndDate);
        RuleFor(x => x.JudgingEndDate).GreaterThanOrEqualTo(x => x.JudgingStartDate);
        RuleFor(x => x.JudgingEndDate).LessThanOrEqualTo(x => x.EventEndDate);
    }
}
