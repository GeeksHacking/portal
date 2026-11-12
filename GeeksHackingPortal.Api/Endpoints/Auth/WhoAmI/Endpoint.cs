using FastEndpoints;
using FastEndpoints.Mcp;
using FastEndpoints.A2A;
using GeeksHackingPortal.Api.Constants;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Auth.WhoAmI;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("auth/whoami");
        Description(b => b.WithTags("Auth"));
        Summary(s =>
        {
            s.Summary = "Get current user info";
            s.Description =
                "Returns the current authenticated user's information including GitHub details.";
        });

        this.McpTool(
            name: "whoami",
            description: "Returns the current authenticated user's profile and GitHub identity.",
            configure: tool =>
            {
                tool.Title = "Who Am I";
                tool.Hints.ReadOnly = true;
                tool.Hints.Idempotent = true;
            });

        this.A2ASkill(
            id: "whoami",
            tags: ["auth", "identity", "read"],
            configure: skill =>
            {
                skill.Name = "Who Am I";
                skill.Description = "Returns the current authenticated user's profile and GitHub identity.";
                skill.Examples = ["Who am I?", "What is my user info?"];
            });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var gitHubAccountId = User
            .Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.GitHubAccountId)
            ?.Value;

        if (string.IsNullOrWhiteSpace(gitHubAccountId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var gh = await sql.Queryable<GitHubOnlineAccount>()
            .Includes(g => g.User)
            .Where(a => a.Id.ToString() == gitHubAccountId)
            .FirstAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = gh.User.Id,
                Name = gh.User.Name,
                FirstName = gh.User.FirstName,
                LastName = gh.User.LastName,
                Email = gh.User.Email,
                GitHubId = gh.GitHubId,
                GitHubLogin = gh.GitHubLogin,
                IsRoot = membership.IsAdmin(gh.User),
            },
            ct
        );
    }
}
