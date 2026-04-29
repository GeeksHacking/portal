using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Registration.Questions.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/standalone-workshops/{StandaloneWorkshopId:guid}/registration/questions");
        Policies(PolicyNames.ParticipantForActivity);
        Description(b => b.WithTags("Participants", "Standalone Workshops", "Registration"));
        Summary(s =>
        {
            s.Summary = "List standalone workshop registration questions";
            s.Description =
                "Gets all registration questions for a standalone workshop, grouped by category, with the current user's submissions if any.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var workshop = await sql.Queryable<Entities.StandaloneWorkshop>()
            .Includes(w => w.Activity)
            .InSingleAsync(req.StandaloneWorkshopId);
        if (workshop is null || !workshop.Activity.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new UnauthorizedAccessException();
        }

        var registration = await sql.Queryable<ActivityRegistration>()
            .SingleAsync(r => r.UserId == userId.Value && r.ActivityId == req.StandaloneWorkshopId);

        if (registration is null)
        {
            AddError("User must be registered for this standalone workshop before seeing its questions.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var questions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.StandaloneWorkshopId)
            .Includes(q => q.Options)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync(ct);

        var submissions = await sql.Queryable<ParticipantRegistrationSubmission>()
            .Where(s => s.ActivityRegistrationId == registration.Id)
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
                                    .. q.Options.OrderBy(o => o.DisplayOrder).Select(o => new OptionDto
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
