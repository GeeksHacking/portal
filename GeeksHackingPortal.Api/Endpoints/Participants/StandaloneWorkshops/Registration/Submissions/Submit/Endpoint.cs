using FastEndpoints;
using FluentValidation.Results;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Registration.Submissions.Submit;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/standalone-workshops/{StandaloneWorkshopId:guid}/registration/submissions");
        Policies(PolicyNames.ParticipantForActivity);
        Description(b => b.WithTags("Submissions").ProducesProblemFE());
        Summary(s =>
        {
            s.Summary = "Submit standalone workshop registration responses";
            s.Description =
                "Submits answers to standalone workshop registration questions. Completed registrations cannot be modified.";
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
            AddError("User must be registered for this standalone workshop before submitting responses.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var allRequiredQuestions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.StandaloneWorkshopId && q.IsRequired)
            .ToListAsync(ct);

        var existingSubmissions = await sql.Queryable<ParticipantRegistrationSubmission>()
            .Where(s => s.ActivityRegistrationId == registration.Id)
            .ToListAsync(ct);

        var existingAnsweredQuestionIds = existingSubmissions.Select(s => s.QuestionId).ToHashSet();
        var isRegistrationAlreadyComplete = allRequiredQuestions.All(q => existingAnsweredQuestionIds.Contains(q.Id));
        if (isRegistrationAlreadyComplete)
        {
            AddError("Registration has already been completed and can no longer be modified.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var questionIds = req.Submissions.Select(s => s.QuestionId).ToList();
        var questions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.StandaloneWorkshopId && questionIds.Contains(q.Id))
            .ToListAsync(ct);

        var questionsById = questions.ToDictionary(q => q.Id);

        var invalidIds = questionIds.Where(id => !questionsById.ContainsKey(id)).ToList();
        if (invalidIds.Count > 0)
        {
            AddError("Some question IDs are invalid or don't belong to this standalone workshop.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        foreach (var submission in req.Submissions)
        {
            if (!questionsById.TryGetValue(submission.QuestionId, out var question))
            {
                continue;
            }

            var validationResult = RegistrationValidationService.Validate(question, submission.Value);
            if (!validationResult.IsValid)
            {
                if (validationResult.ErrorsByQuestionId.Count > 0)
                {
                    foreach (var (questionId, errors) in validationResult.ErrorsByQuestionId)
                    {
                        var fieldKey = questionId.ToString();
                        foreach (var error in errors)
                        {
                            AddError(new ValidationFailure(fieldKey, error));
                        }
                    }
                }
                else
                {
                    foreach (var error in validationResult.Errors)
                    {
                        AddError(error);
                    }
                }
            }
        }

        if (ValidationFailed)
        {
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var submittedQuestionIds = req.Submissions.Select(s => s.QuestionId).ToHashSet();
        var missingRequired = allRequiredQuestions
            .Where(q => !submittedQuestionIds.Contains(q.Id))
            .Select(q => q.QuestionKey)
            .ToList();

        using var tran = sql.Ado.UseTran();

        await sql.Deleteable<ParticipantRegistrationSubmission>()
            .Where(s => s.ActivityRegistrationId == registration.Id && questionIds.Contains(s.QuestionId))
            .ExecuteCommandAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var submissions = req.Submissions.Select(s => new ParticipantRegistrationSubmission
        {
            Id = Guid.NewGuid(),
            ActivityRegistrationId = registration.Id,
            QuestionId = s.QuestionId,
            Value = s.Value,
            FollowUpValue = s.FollowUpValue,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();

        await sql.Insertable(submissions).ExecuteCommandAsync(ct);

        tran.CommitTran();

        await Send.OkAsync(
            new Response
            {
                SubmissionsCount = submissions.Count,
                Message = missingRequired.Count > 0
                    ? $"Submissions saved. Note: {missingRequired.Count} required questions are still unanswered."
                    : "All submissions saved successfully.",
            },
            ct
        );
    }
}
