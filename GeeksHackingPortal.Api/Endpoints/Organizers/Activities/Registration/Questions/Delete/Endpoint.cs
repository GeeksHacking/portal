using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Registration.Questions.Delete;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete(
            "organizers/hackathons/{ActivityId:guid}/registration/questions/{QuestionId:guid}",
            "organizers/standalone-workshops/{ActivityId:guid}/registration/questions/{QuestionId:guid}"
        );
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Registration"));
        Summary(s =>
        {
            s.Summary = "Delete an activity registration question";
            s.Description =
                "Delete a registration question and all its options. This will also delete any participant submissions for this question.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var question = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.Id == req.QuestionId && q.ActivityId == req.ActivityId)
            .FirstAsync(ct);

        if (question is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        using var tran = sql.Ado.UseTran();

        await sql.Deleteable<ParticipantRegistrationSubmission>()
            .Where(s => s.QuestionId == question.Id)
            .ExecuteCommandAsync(ct);

        await sql.Deleteable<RegistrationQuestionOption>()
            .Where(o => o.QuestionId == question.Id)
            .ExecuteCommandAsync(ct);

        await sql.Deleteable(question).ExecuteCommandAsync(ct);

        tran.CommitTran();

        await Send.NoContentAsync(ct);
    }
}
