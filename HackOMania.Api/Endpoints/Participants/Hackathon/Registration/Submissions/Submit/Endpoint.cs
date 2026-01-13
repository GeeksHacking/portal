using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Registration.Submissions.Submit;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/registration/submissions");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Registration"));
        Summary(s =>
        {
            s.Summary = "Submit registration responses";
            s.Description =
                "Submit or update answers to registration questions. Existing answers will be replaced.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.IsPublished)
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

        // Validate that all questions exist and are for this hackathon
        var questionIds = req.Submissions.Select(s => s.QuestionId).ToList();
        var questions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.HackathonId == req.HackathonId && questionIds.Contains(q.Id))
            .ToListAsync(ct);

        var questionsById = questions.ToDictionary(q => q.Id);

        // Check for invalid question IDs
        var invalidIds = questionIds.Where(id => !questionsById.ContainsKey(id)).ToList();
        if (invalidIds.Count > 0)
        {
            AddError("Some question IDs are invalid or don't belong to this hackathon.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Validate each submission against the question's validation rules
        foreach (var submission in req.Submissions)
        {
            if (!questionsById.TryGetValue(submission.QuestionId, out var question))
            {
                continue;
            }

            var validationResult = RegistrationValidationService.Validate(
                question,
                submission.Value
            );
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    AddError(error);
                }
            }
        }

        // Return errors if any validation failed
        if (ValidationFailed)
        {
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Check required questions
        var allRequiredQuestions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.HackathonId == req.HackathonId && q.IsRequired)
            .ToListAsync(ct);

        var submittedQuestionIds = req.Submissions.Select(s => s.QuestionId).ToHashSet();
        var missingRequired = allRequiredQuestions
            .Where(q => !submittedQuestionIds.Contains(q.Id))
            .Select(q => q.QuestionKey)
            .ToList();

        // Delete existing submissions for the questions being updated
        await sql.Deleteable<ParticipantRegistrationSubmission>()
            .Where(s => s.ParticipantId == participant.Id && questionIds.Contains(s.QuestionId))
            .ExecuteCommandAsync(ct);

        // Insert new submissions
        var now = DateTimeOffset.UtcNow;
        var submissions = req
            .Submissions.Select(s => new ParticipantRegistrationSubmission
            {
                Id = Guid.NewGuid(),
                ParticipantId = participant.Id,
                QuestionId = s.QuestionId,
                Value = s.Value,
                FollowUpValue = s.FollowUpValue,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

        await sql.Insertable(submissions).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                SubmissionsCount = submissions.Count,
                Message =
                    missingRequired.Count > 0
                        ? $"Submissions saved. Note: {missingRequired.Count} required questions are still unanswered."
                        : "All submissions saved successfully.",
            },
            ct
        );
    }
}
