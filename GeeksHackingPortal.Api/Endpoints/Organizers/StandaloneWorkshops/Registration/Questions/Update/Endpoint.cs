using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Registration.Questions.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/registration/questions/{QuestionId:guid}");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Registration", "Standalone Workshops"));
        Summary(s => s.Summary = "Update standalone workshop registration question");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var question = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.Id == req.QuestionId && q.ActivityId == req.StandaloneWorkshopId)
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

            question.Options =
            [
                .. req.Options.Select(o => new RegistrationQuestionOption
                {
                    Id = o.Id ?? Guid.NewGuid(),
                    QuestionId = question.Id,
                    OptionText = o.OptionText,
                    OptionValue = o.OptionValue,
                    DisplayOrder = o.DisplayOrder,
                    HasFollowUpText = o.HasFollowUpText,
                    FollowUpPlaceholder = o.FollowUpPlaceholder,
                }),
            ];

            if (question.Options.Count > 0)
            {
                await sql.Insertable(question.Options).ExecuteCommandAsync(ct);
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
