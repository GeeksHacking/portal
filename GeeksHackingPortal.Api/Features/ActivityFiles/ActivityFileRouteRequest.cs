using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Features.ActivityFiles;

/// <summary>
/// Route values that identify the activity a file belongs to. Activity file endpoints are mapped to one route per
/// activity kind, and each route supplies the ids for its kind:
/// <list type="bullet">
/// <item><c>hackathons/{HackathonId}</c></item>
/// <item><c>hackathons/{HackathonId}/workshops/{WorkshopId}</c></item>
/// <item><c>standalone-workshops/{StandaloneWorkshopId}</c></item>
/// </list>
/// </summary>
public abstract class ActivityFileRouteRequest
{
    /// <summary>
    /// The route templates for an activity file endpoint, e.g. <c>Routes("organizers", "banner")</c>.
    /// </summary>
    /// <remarks>
    /// The id properties are hidden from the OpenAPI document, otherwise GET operations would list the ids of the
    /// other routes as query parameters. Route values bind case-insensitively, but hidden properties are no longer
    /// renamed in the document's paths, so the templates are written in camelCase to match the parameter names.
    /// </remarks>
    public static string[] Routes(string audience, string suffix) =>
    [
        $"{audience}/hackathons/{{hackathonId:guid}}/{suffix}",
        $"{audience}/hackathons/{{hackathonId:guid}}/workshops/{{workshopId:guid}}/{suffix}",
        $"{audience}/standalone-workshops/{{standaloneWorkshopId:guid}}/{suffix}",
    ];

    [RouteParam, HideFromDocs]
    public Guid? HackathonId { get; set; }

    [RouteParam, HideFromDocs]
    public Guid? WorkshopId { get; set; }

    [RouteParam, HideFromDocs]
    public Guid? StandaloneWorkshopId { get; set; }

    /// <summary>
    /// Loads the activity named by the route, or null when it does not exist, is of a different kind than the
    /// route expects, or (for a hackathon workshop) belongs to a different hackathon.
    /// </summary>
    public async Task<Activity?> FindActivityAsync(ISqlSugarClient sql, CancellationToken ct)
    {
        Guid activityId;
        ActivityKind kind;

        if (WorkshopId is { } workshopId)
        {
            var belongsToHackathon = await sql.Queryable<Workshop>()
                .AnyAsync(w => w.Id == workshopId && w.HackathonId == HackathonId, ct);
            if (!belongsToHackathon)
            {
                return null;
            }

            (activityId, kind) = (workshopId, ActivityKind.HackathonWorkshop);
        }
        else if (StandaloneWorkshopId is { } standaloneWorkshopId)
        {
            (activityId, kind) = (standaloneWorkshopId, ActivityKind.StandaloneWorkshop);
        }
        else if (HackathonId is { } hackathonId)
        {
            (activityId, kind) = (hackathonId, ActivityKind.Hackathon);
        }
        else
        {
            return null;
        }

        return await sql.Queryable<Activity>()
            .Where(a => a.Id == activityId && a.Kind == kind)
            .FirstAsync(ct);
    }
}
