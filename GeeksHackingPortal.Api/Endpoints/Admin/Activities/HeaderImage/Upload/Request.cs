namespace GeeksHackingPortal.Api.Endpoints.Admin.Activities.HeaderImage.Upload;

public class Request
{
    public Guid ActivityId { get; set; }

    /// <summary>Object key returned by POST /admin/activities/{activityId}/header-image/upload-url.</summary>
    public string Key { get; set; } = string.Empty;
}
