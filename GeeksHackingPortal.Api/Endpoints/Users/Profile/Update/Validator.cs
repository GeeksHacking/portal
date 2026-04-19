using FastEndpoints;
using FluentValidation;

namespace GeeksHackingPortal.Api.Endpoints.Users.Profile.Update;

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.FirstName).Must(value => !string.IsNullOrWhiteSpace(value));
        RuleFor(x => x.LastName).Must(value => !string.IsNullOrWhiteSpace(value));
    }
}
