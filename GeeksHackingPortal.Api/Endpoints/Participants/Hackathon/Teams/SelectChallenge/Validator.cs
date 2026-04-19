using FastEndpoints;
using FluentValidation;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Teams.SelectChallenge;

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.HackathonId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.ChallengeId).NotEmpty();
    }
}
