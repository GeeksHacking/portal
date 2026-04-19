using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Users.Profile.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("users/me");
        Description(b => b.WithTags("Users"));
        Summary(s =>
        {
            s.Summary = "Update current user profile";
            s.Description = "Updates the current user's first and last name.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = await sql.Queryable<User>().InSingleAsync(userId.Value);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        user.FirstName = req.FirstName.Trim();
        user.LastName = req.LastName.Trim();

        await sql.Updateable(user)
            .UpdateColumns(u => new { u.FirstName, u.LastName })
            .ExecuteCommandAsync(ct);

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
