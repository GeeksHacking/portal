using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Registration.Questions.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/registration/questions");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Registration"));
        Summary(s =>
        {
            s.Summary = "List registration questions";
            s.Description = "Get all registration questions for a hackathon with their options.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var questions = await sql.Queryable<RegistrationQuestion>()
            .Where(q => q.HackathonId == req.HackathonId)
            .Includes(q => q.Options)
            .OrderBy(q => q.DisplayOrder)
            .WithCache()
            .ToListAsync(ct);

        var response = new Response
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
                        .. q
                            .Options.OrderBy(o => o.DisplayOrder)
                            .Select(o => new OptionDto
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
        };

        await Send.OkAsync(response, ct);
    }
}
