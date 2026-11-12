using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

/// <summary>
/// A file attached to an <see cref="Activity"/>. The bytes live in object storage under <see cref="StorageKey"/>.
/// An activity has at most one <see cref="ActivityFileKind.Banner"/>.
/// </summary>
[SugarIndex("IX_ActivityFile_ActivityId", nameof(ActivityId), OrderByType.Asc)]
public class ActivityFile
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid ActivityId { get; set; }

    public ActivityFileKind Kind { get; set; }

    /// <summary>
    /// The name the file was uploaded with, used when it is downloaded.
    /// </summary>
    public string FileName { get; set; } = null!;

    public string StorageKey { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    public Guid UploadedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Navigate(NavigateType.ManyToOne, nameof(ActivityId))]
    public Activity Activity { get; set; } = null!;
}

public enum ActivityFileKind
{
    Banner = 1,
    Attachment = 2,
}
