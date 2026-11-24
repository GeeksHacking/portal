namespace GeeksHackingPortal.Api.Endpoints.Admin.Activities.HeaderImage.UploadUrl;

public class Request
{
    public Guid ActivityId { get; set; }

    /// <summary>MIME type of the image: image/jpeg, image/png or image/webp.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Size of the image in bytes (max 5 MB).</summary>
    public int ContentLength { get; set; }
}
