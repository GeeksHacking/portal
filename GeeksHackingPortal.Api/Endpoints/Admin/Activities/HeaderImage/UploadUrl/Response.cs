namespace GeeksHackingPortal.Api.Endpoints.Admin.Activities.HeaderImage.UploadUrl;

public class Response
{
    /// <summary>Presigned URL to PUT the image to, with the same Content-Type header.</summary>
    public required string UploadUrl { get; init; }

    /// <summary>Object key to pass to PUT /admin/activities/{activityId}/header-image once the upload is done.</summary>
    public required string Key { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
