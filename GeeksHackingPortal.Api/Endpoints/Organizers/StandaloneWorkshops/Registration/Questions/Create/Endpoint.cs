using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Registration.Questions.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/registration/questions");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Registration", "Standalone Workshops"));
        Summary(s => s.Summary = "Create standalone workshop registration question");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var exists = await sql.Queryable<StandaloneWorkshop>()
            .AnyAsync(w => w.Id == req.StandaloneWorkshopId, ct);
        if (!exists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var existingKey = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.StandaloneWorkshopId && q.QuestionKey == req.QuestionKey)
            .AnyAsync(ct);
        if (existingKey)
        {
            AddError(r => r.QuestionKey, "A question with this key already exists.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var question = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            ActivityId = req.StandaloneWorkshopId,
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

        var options = req.Options?.Select(o => new RegistrationQuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            OptionText = o.OptionText,
            OptionValue = o.OptionValue,
            DisplayOrder = o.DisplayOrder,
            HasFollowUpText = o.HasFollowUpText,
            FollowUpPlaceholder = o.FollowUpPlaceholder,
        }).ToList() ?? [];

        var transactionResult = await sql.Ado.UseTranAsync(async () =>
        {
            await sql.Insertable(question).ExecuteCommandAsync(ct);
            if (options.Count > 0)
            {
                await sql.Insertable(options).ExecuteCommandAsync(ct);
            }
        });

        if (!transactionResult.IsSuccess)
        {
            throw transactionResult.ErrorException!;
        }

        await Send.OkAsync(ToResponse(question, options), ct);
    }

    private static Response ToResponse(
        RegistrationQuestion question,
        IEnumerable<RegistrationQuestionOption> options
    ) =>
        new()
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
        };
}
