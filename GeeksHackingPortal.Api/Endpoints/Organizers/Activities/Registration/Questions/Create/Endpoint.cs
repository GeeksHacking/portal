using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Registration.Questions.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post(
            "organizers/hackathons/{ActivityId:guid}/registration/questions",
            "organizers/standalone-workshops/{ActivityId:guid}/registration/questions"
        );
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Registration"));
        Summary(s =>
        {
            s.Summary = "Create an activity registration question";
            s.Description = "Create a new registration question for participants to answer.";
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

        var existingKey = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.ActivityId && q.QuestionKey == req.QuestionKey)
            .AnyAsync(ct);

        if (existingKey)
        {
            AddError(r => r.QuestionKey, "A question with this key already exists for this activity.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var question = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            ActivityId = req.ActivityId,
            QuestionText = req.QuestionText,
            QuestionKey = req.QuestionKey,
            Type = req.Type,
            DisplayOrder = req.DisplayOrder,
            IsRequired = req.IsRequired,
            HelpText = req.HelpText,
            ConditionalLogic = req.ConditionalLogic,
            Category = req.Category,
            ValidationRules = req.ValidationRules,
        };

        using var tran = sql.Ado.UseTran();

        await sql.Insertable(question).ExecuteCommandAsync(ct);

        var options = new List<RegistrationQuestionOption>();
        if (req.Options is { Count: > 0 })
        {
            options =
            [
                .. req.Options.Select(o => new RegistrationQuestionOption
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    OptionText = o.OptionText,
                    OptionValue = o.OptionValue,
                    DisplayOrder = o.DisplayOrder,
                    HasFollowUpText = o.HasFollowUpText,
                    FollowUpPlaceholder = o.FollowUpPlaceholder,
                }),
            ];

            await sql.Insertable(options).ExecuteCommandAsync(ct);
        }

        tran.CommitTran();

        await Send.CreatedAtAsync<Endpoint>(
            new { req.ActivityId, QuestionId = question.Id },
            new Response
            {
                Id = question.Id,
                QuestionText = question.QuestionText,
                QuestionKey = question.QuestionKey,
                Type = question.Type,
                DisplayOrder = question.DisplayOrder,
                IsRequired = question.IsRequired,
                HelpText = question.HelpText,
                ConditionalLogic = question.ConditionalLogic,
                Category = question.Category,
                ValidationRules = question.ValidationRules,
                Options =
                [
                    .. options.Select(o => new OptionResponse
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        OptionValue = o.OptionValue,
                        DisplayOrder = o.DisplayOrder,
                        HasFollowUpText = o.HasFollowUpText,
                        FollowUpPlaceholder = o.FollowUpPlaceholder,
                    }),
                ],
            },
            cancellation: ct
        );
    }
}
