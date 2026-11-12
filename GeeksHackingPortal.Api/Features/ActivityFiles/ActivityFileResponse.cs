using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Storage;

namespace GeeksHackingPortal.Api.Features.ActivityFiles;

public class ActivityFileResponse
{
    public required Guid Id { get; init; }
    public required ActivityFileKind Kind { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Time-limited download URL. Fetch a fresh one instead of storing it.
    /// </summary>
    public required string Url { get; init; }

    public static ActivityFileResponse From(ActivityFile file, ActivityFileStorage storage) =>
        new()
        {
            Id = file.Id,
            Kind = file.Kind,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            CreatedAt = file.CreatedAt,
            Url = storage.GetDownloadUrl(file),
        };
}
