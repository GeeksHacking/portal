using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Registration.Questions.Initialize;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post(
            "organizers/hackathons/{ActivityId:guid}/registration/questions/initialize",
            "organizers/standalone-workshops/{ActivityId:guid}/registration/questions/initialize"
        );
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Registration").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Initialize standard activity registration questions";
            s.Description =
                "Create a standard set of registration questions for the activity. Will not create if questions already exist.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var exists = await sql.Queryable<Activity>().AnyAsync(a => a.Id == req.ActivityId, ct);
        if (!exists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var existingCount = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.ActivityId)
            .CountAsync(ct);

        if (existingCount > 0)
        {
            await Send.OkAsync(
                new Response
                {
                    QuestionsCreated = 0,
                    Message =
                        $"Questions already exist for this activity ({existingCount} questions found). Delete existing questions first if you want to reinitialize.",
                },
                ct
            );
            return;
        }

        var questionsWithOptions = RegistrationQuestionTemplateService.CreateStandardQuestions(
            req.ActivityId
        );

        using var tran = sql.Ado.UseTran();

        foreach (var (question, options) in questionsWithOptions)
        {
            await sql.Insertable(question).ExecuteCommandAsync(ct);

            if (options.Count > 0)
            {
                await sql.Insertable(options).ExecuteCommandAsync(ct);
            }
        }

        tran.CommitTran();

        await Send.CreatedAtAsync<List.Endpoint>(
            new { req.ActivityId },
            new Response
            {
                QuestionsCreated = questionsWithOptions.Count,
                Message =
                    $"Successfully created {questionsWithOptions.Count} standard registration questions.",
            },
            cancellation: ct
        );
    }
}
