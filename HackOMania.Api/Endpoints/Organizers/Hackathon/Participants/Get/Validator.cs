using FastEndpoints;
using FluentValidation;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Participants.Get;

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.HackathonId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
