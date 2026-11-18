namespace GeeksHackingPortal.Api.Endpoints.Admin.Activities.HeaderImage.Upload;

public class Request
{
    public Guid ActivityId { get; set; }
    public IFormFile File { get; set; } = null!;
}
