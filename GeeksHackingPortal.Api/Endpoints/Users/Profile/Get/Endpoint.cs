using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Users.Profile.Get;

public class Endpoint(ISqlSugarClient sql) : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("users/me");
        Description(b => b.WithTags("Users"));
        Summary(s =>
        {
            s.Summary = "Get current user profile";
            s.Description = "Returns the current user's profile name information.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = await sql.Queryable<User>().WithCache().InSingleAsync(userId.Value);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(
            new Response
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Name = user.Name,
            },
            ct
        );
    }
}
