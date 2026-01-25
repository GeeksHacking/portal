using FastEndpoints;
using FluentValidation;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.SelectChallenge;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid TeamId { get; set; }
    public Guid ChallengeId { get; set; }
}

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.HackathonId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.ChallengeId).NotEmpty();
    }
}
