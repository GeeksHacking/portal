using FastEndpoints;
using FluentValidation;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Registration.Questions.Create;

public class Validator : Validator<Request>
{
    /// <summary>
    /// A participant's name is part of their user profile, and every sign-up form asks for it,
    /// so it must not also be collected as a per-activity registration answer.
    /// </summary>
    private static readonly HashSet<string> ProfileNameKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "first_name",
        "last_name",
    };

    public Validator()
    {
        RuleFor(x => x.QuestionKey)
            .Must(key => key is null || !ProfileNameKeys.Contains(key.Trim()))
            .WithMessage(
                "Participant names come from each user's profile and are collected on every sign-up form, so this question key is reserved."
            );
    }
}
