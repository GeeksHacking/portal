using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Registration.Submissions.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/standalone-workshops/{StandaloneWorkshopId:guid}/registration/submissions");
        Policies(PolicyNames.ParticipantForActivity);
        Description(b => b.WithTags("Submissions"));
        Summary(s =>
        {
            s.Summary = "Get my standalone workshop registration submissions";
            s.Description = "Gets the current user's registration submissions for a standalone workshop.";
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
            AddError("User must be registered for this standalone workshop before seeing submissions.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var submissions = await sql.Queryable<ParticipantRegistrationSubmission>()
            .Where(s => s.ActivityRegistrationId == registration.Id)
            .Includes(s => s.Question)
            .ToListAsync(ct);

        var totalQuestions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.StandaloneWorkshopId)
            .CountAsync(ct);

        var requiredQuestions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.StandaloneWorkshopId && q.IsRequired)
            .Select(q => q.Id)
            .ToListAsync(ct);

        var answeredQuestionIds = submissions.Select(s => s.QuestionId).ToHashSet();
        var requiredRemaining = requiredQuestions.Count(id => !answeredQuestionIds.Contains(id));

        await Send.OkAsync(
            new Response
            {
                Submissions =
                [
                    .. submissions.Select(s => new SubmissionDto
                    {
                        QuestionId = s.QuestionId,
                        QuestionKey = s.Question.QuestionKey,
                        QuestionText = s.Question.QuestionText,
                        Category = s.Question.Category,
                        Value = s.Value,
                        FollowUpValue = s.FollowUpValue,
                        UpdatedAt = s.UpdatedAt,
                    }),
                ],
                TotalQuestions = totalQuestions,
                AnsweredQuestions = submissions.Count,
                RequiredQuestionsRemaining = requiredRemaining,
            },
            ct
        );
    }
}
