using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Registration.Questions.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId:guid}/registration/questions");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Registration"));
        Summary(s =>
        {
            s.Summary = "List registration questions";
            s.Description =
                "Get all registration questions for a hackathon, grouped by category, with the current user's submissions if any.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity).InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.Activity.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new UnauthorizedAccessException();
        }

        var participant = await sql.Queryable<Participant>()
            .SingleAsync(p => p.UserId == userId.Value && p.HackathonId == req.HackathonId);

        // User can access this route if they are an organizer as well
        if (participant is null)
        {
            AddError("User must be a participant of the hackathon before seeing its questions!");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var questions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.HackathonId)
            .Includes(q => q.Options)
            .OrderBy(q => q.DisplayOrder)
            
            .ToListAsync(ct);

        var submissions = await sql.Queryable<ParticipantRegistrationSubmission>()
            .Where(s => s.ActivityRegistrationId == participant.Id)
            .ToListAsync(ct);

        var submissionsByQuestionId = submissions.ToDictionary(s => s.QuestionId);

        var grouped = questions
            .GroupBy(q => q.Category ?? "Other")
            .OrderBy(g => questions.Where(q => q.Category == g.Key).Min(q => q.DisplayOrder))
            .Select(g => new CategoryDto
            {
                Name = g.Key,
                Questions =
                [
                    .. g.OrderBy(q => q.DisplayOrder)
                        .Select(q =>
                        {
                            submissionsByQuestionId.TryGetValue(q.Id, out var submission);
                            return new QuestionDto
                            {
                                Id = q.Id,
                                QuestionText = q.QuestionText,
                                QuestionKey = q.QuestionKey,
                                Type = q.Type,
                                IsRequired = q.IsRequired,
                                HelpText = q.HelpText,
                                ConditionalLogic = q.ConditionalLogic,
                                ValidationRules = q.ValidationRules,
                                Options =
                                [
                                    .. q
                                        .Options.OrderBy(o => o.DisplayOrder)
                                        .Select(o => new OptionDto
                                        {
                                            Id = o.Id,
                                            OptionText = o.OptionText,
                                            OptionValue = o.OptionValue,
                                            HasFollowUpText = o.HasFollowUpText,
                                            FollowUpPlaceholder = o.FollowUpPlaceholder,
                                        }),
                                ],
                                CurrentSubmission = submission is not null
                                    ? new SubmissionDto
                                    {
                                        Value = submission.Value,
                                        FollowUpValue = submission.FollowUpValue,
                                    }
                                    : null,
                            };
                        }),
                ],
            })
            .ToList();

        await Send.OkAsync(new Response { Categories = grouped }, ct);
    }
}
