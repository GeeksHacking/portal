using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Registration.Questions.Delete;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/registration/questions/{QuestionId:guid}");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Registration", "Standalone Workshops"));
        Summary(s => s.Summary = "Delete standalone workshop registration question");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var question = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.Id == req.QuestionId && q.ActivityId == req.StandaloneWorkshopId)
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
