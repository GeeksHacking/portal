using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public class Workshop
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid HackathonId { get; set; }

    [SugarColumn(IsIgnore = true)]
    public Guid ActivityId
    {
        get => Id;
        set
        {
            if (Id == Guid.Empty)
            {
                Id = value;
            }
        }
    }

    [Navigate(NavigateType.OneToOne, nameof(Id))]
    public Activity Activity { get; set; } = null!;

    [SugarColumn(ColumnName = "Title", IsNullable = true)]
    public string? LegacyTitle { get; set; }

    [SugarColumn(IsIgnore = true)]
    public string Title
    {
        get => Activity?.Title ?? LegacyTitle ?? string.Empty;
        set
        {
            EnsureActivity().Title = value;
            LegacyTitle = value;
        }
    }

    [SugarColumn(ColumnName = "Description", IsNullable = true, ColumnDataType = "longtext")]
    public string? LegacyDescription { get; set; }

    [SugarColumn(IsIgnore = true)]
    public string Description
    {
        get => Activity?.Description ?? LegacyDescription ?? string.Empty;
        set
        {
            EnsureActivity().Description = value;
            LegacyDescription = value;
        }
    }

    [SugarColumn(ColumnName = "StartTime")]
    public DateTimeOffset LegacyStartTime { get; set; }

    [SugarColumn(IsIgnore = true)]
    public DateTimeOffset StartTime
    {
        get => Activity?.StartTime ?? LegacyStartTime;
        set
        {
            EnsureActivity().StartTime = value;
            LegacyStartTime = value;
        }
    }

    [SugarColumn(ColumnName = "EndTime")]
    public DateTimeOffset LegacyEndTime { get; set; }

    [SugarColumn(IsIgnore = true)]
    public DateTimeOffset EndTime
    {
        get => Activity?.EndTime ?? LegacyEndTime;
        set
        {
            EnsureActivity().EndTime = value;
            LegacyEndTime = value;
        }
    }

    [SugarColumn(ColumnName = "Location", IsNullable = true)]
    public string? LegacyLocation { get; set; }

    [SugarColumn(IsIgnore = true)]
    public string Location
    {
        get => Activity?.Location ?? LegacyLocation ?? string.Empty;
        set
        {
            EnsureActivity().Location = value;
            LegacyLocation = value;
        }
    }

    public int MaxParticipants { get; set; }

    [SugarColumn(ColumnName = "IsPublished")]
    public bool LegacyIsPublished { get; set; }

    [SugarColumn(IsIgnore = true)]
    public bool IsPublished
    {
        get => Activity?.IsPublished ?? LegacyIsPublished;
        set
        {
            EnsureActivity().IsPublished = value;
            LegacyIsPublished = value;
        }
    }

    [SugarColumn(ColumnName = "CreatedAt")]
    public DateTimeOffset LegacyCreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [SugarColumn(IsIgnore = true)]
    public DateTimeOffset CreatedAt
    {
        get => Activity?.CreatedAt ?? LegacyCreatedAt;
        set
        {
            EnsureActivity().CreatedAt = value;
            LegacyCreatedAt = value;
        }
    }

    [SugarColumn(ColumnName = "UpdatedAt")]
    public DateTimeOffset LegacyUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [SugarColumn(IsIgnore = true)]
    public DateTimeOffset UpdatedAt
    {
        get => Activity?.UpdatedAt ?? LegacyUpdatedAt;
        set
        {
            EnsureActivity().UpdatedAt = value;
            LegacyUpdatedAt = value;
        }
    }

    [Navigate(NavigateType.OneToMany, nameof(WorkshopParticipant.WorkshopId))]
    public List<WorkshopParticipant> Participants { get; set; } = null!;

    public Activity EnsureActivity()
    {
        if (Activity is not null)
        {
            return Activity;
        }

        var activityId = Id;
        if (activityId == Guid.Empty)
        {
            activityId = Guid.NewGuid();
            Id = activityId;
        }

        Activity = new Activity
        {
            Id = activityId,
            Kind = ActivityKind.HackathonWorkshop,
            Title = LegacyTitle ?? string.Empty,
            Description = LegacyDescription ?? string.Empty,
            Location = LegacyLocation ?? string.Empty,
            StartTime = LegacyStartTime,
            EndTime = LegacyEndTime,
            IsPublished = LegacyIsPublished,
            CreatedAt = LegacyCreatedAt,
            UpdatedAt = LegacyUpdatedAt,
        };
        return Activity;
    }
}
