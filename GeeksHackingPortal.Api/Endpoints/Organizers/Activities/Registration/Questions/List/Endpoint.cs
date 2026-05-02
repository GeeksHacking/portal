using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Registration.Questions.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get(
            "organizers/hackathons/{ActivityId:guid}/registration/questions",
            "organizers/standalone-workshops/{ActivityId:guid}/registration/questions"
        );
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Registration"));
        Summary(s =>
        {
            s.Summary = "List activity registration questions";
            s.Description = "Get all registration questions for an activity with their options.";
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

        var questions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.ActivityId == req.ActivityId)
            .Includes(q => q.Options)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Questions =
                [
                    .. questions.Select(q => new QuestionDto
                    {
                        Id = q.Id,
                        QuestionText = q.QuestionText,
                        QuestionKey = q.QuestionKey,
                        Type = q.Type,
                        DisplayOrder = q.DisplayOrder,
                        IsRequired = q.IsRequired,
                        HelpText = q.HelpText,
                        ConditionalLogic = q.ConditionalLogic,
                        Category = q.Category,
                        ValidationRules = q.ValidationRules,
                        Options =
                        [
                            .. q.Options.OrderBy(o => o.DisplayOrder).Select(o => new OptionDto
                            {
                                Id = o.Id,
                                OptionText = o.OptionText,
                                OptionValue = o.OptionValue,
                                DisplayOrder = o.DisplayOrder,
                                HasFollowUpText = o.HasFollowUpText,
                                FollowUpPlaceholder = o.FollowUpPlaceholder,
                            }),
                        ],
                    }),
                ],
            },
            ct
        );
    }
}
