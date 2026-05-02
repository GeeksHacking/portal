using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Registration.Submissions.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId:guid}/registration/submissions");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Submissions"));
        Summary(s =>
        {
            s.Summary = "Get my registration submissions";
            s.Description = "Get the current user's registration submissions for a hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Includes(h => h.Activity)
            
            .InSingleAsync(req.HackathonId);
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

        var submissions = await sql.Queryable<ParticipantRegistrationSubmission>()
            .Where(s => s.ActivityRegistrationId == participant.Id)
            .Includes(s => s.Question)
            .ToListAsync(ct);

        var totalQuestions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.HackathonId)
            
            .CountAsync(ct);

        var requiredQuestions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.HackathonId && q.IsRequired)
            
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
