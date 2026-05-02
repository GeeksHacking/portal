using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Registration.Questions.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch(
            "organizers/hackathons/{ActivityId:guid}/registration/questions/{QuestionId:guid}",
            "organizers/standalone-workshops/{ActivityId:guid}/registration/questions/{QuestionId:guid}"
        );
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Registration"));
        Summary(s =>
        {
            s.Summary = "Update an activity registration question";
            s.Description = "Update an existing registration question and its options.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var question = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.Id == req.QuestionId && q.ActivityId == req.ActivityId)
            .Includes(q => q.Options)
            .FirstAsync(ct);

        if (question is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (req.QuestionText is not null)
            question.QuestionText = req.QuestionText;
        if (req.Type.HasValue)
            question.Type = req.Type.Value;
        if (req.DisplayOrder.HasValue)
            question.DisplayOrder = req.DisplayOrder.Value;
        if (req.IsRequired.HasValue)
            question.IsRequired = req.IsRequired.Value;
        if (req.HelpText is not null)
            question.HelpText = req.HelpText;
        if (req.ConditionalLogic is not null)
            question.ConditionalLogic = req.ConditionalLogic;
        if (req.Category is not null)
            question.Category = req.Category;
        if (req.ValidationRules is not null)
            question.ValidationRules = req.ValidationRules;

        using var tran = sql.Ado.UseTran();

        await sql.Updateable(question).ExecuteCommandAsync(ct);

        if (req.Options is not null)
        {
            await sql.Deleteable<RegistrationQuestionOption>()
                .Where(o => o.QuestionId == question.Id)
                .ExecuteCommandAsync(ct);

            if (req.Options.Count > 0)
            {
                var newOptions = req
                    .Options.Select(o => new RegistrationQuestionOption
                    {
                        Id = o.Id ?? Guid.NewGuid(),
                        QuestionId = question.Id,
                        OptionText = o.OptionText,
                        OptionValue = o.OptionValue,
                        DisplayOrder = o.DisplayOrder,
                        HasFollowUpText = o.HasFollowUpText,
                        FollowUpPlaceholder = o.FollowUpPlaceholder,
                    })
                    .ToList();

                await sql.Insertable(newOptions).ExecuteCommandAsync(ct);
                question.Options = newOptions;
            }
            else
            {
                question.Options = [];
            }
        }

        tran.CommitTran();

        await Send.OkAsync(
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
                    .. question.Options.Select(o => new OptionResponse
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
            ct
        );
    }
}
