using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Registration.Questions.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId:guid}/registration/questions");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Registration"));
        Summary(s =>
        {
            s.Summary = "Create a registration question";
            s.Description = "Create a new registration question for participants to answer.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity).InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var existingKey = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.HackathonId && q.QuestionKey == req.QuestionKey)
            .AnyAsync(ct);

        if (existingKey)
        {
            AddError(
                r => r.QuestionKey,
                "A question with this key already exists for this hackathon."
            );
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var question = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            ActivityId = req.HackathonId,
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

        await Send.CreatedAtAsync<Endpoint>(
            new { req.HackathonId, QuestionId = question.Id },
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
