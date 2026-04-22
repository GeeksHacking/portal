using GeeksHackingPortal.Api.Converters;
using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

/// <summary>
/// Workshops/events that are organized independent of a <see cref="Hackathon"/>.
///
/// The term "event" is avoided to avoid conflict with language / runtime features.
/// </summary>
[SugarIndex("IX_StandaloneWorkshop_ShortCode", nameof(ShortCode), OrderByType.Asc)]
public class StandaloneWorkshop
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

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

    [SugarColumn(IsIgnore = true)]
    public string Title
    {
        get => Activity?.Title ?? string.Empty;
        set => EnsureActivity().Title = value;
    }

    [SugarColumn(IsIgnore = true)]
    public string Description
    {
        get => Activity?.Description ?? string.Empty;
        set => EnsureActivity().Description = value;
    }

    [SugarColumn(IsIgnore = true)]
    public DateTimeOffset StartTime
    {
        get => Activity?.StartTime ?? default;
        set => EnsureActivity().StartTime = value;
    }

    [SugarColumn(IsIgnore = true)]
    public DateTimeOffset EndTime
    {
        get => Activity?.EndTime ?? default;
        set => EnsureActivity().EndTime = value;
    }

    [SugarColumn(IsIgnore = true)]
    public string Location
    {
        get => Activity?.Location ?? string.Empty;
        set => EnsureActivity().Location = value;
    }

    [SugarColumn(ColumnDataType = "nvarchar(128)", SqlParameterDbType = typeof(UriConverter))]
    public Uri HomepageUri { get; set; } = null!;

    public string ShortCode { get; set; } = null!;

    public int MaxParticipants { get; set; }

    [SugarColumn(IsIgnore = true)]
    public bool IsPublished
    {
        get => Activity?.IsPublished ?? false;
        set => EnsureActivity().IsPublished = value;
    }

    [SugarColumn(IsIgnore = true)]
    public DateTimeOffset CreatedAt
    {
        get => Activity?.CreatedAt ?? default;
        set => EnsureActivity().CreatedAt = value;
    }

    [SugarColumn(IsIgnore = true)]
    public DateTimeOffset UpdatedAt
    {
        get => Activity?.UpdatedAt ?? default;
        set => EnsureActivity().UpdatedAt = value;
    }

    [Navigate(NavigateType.OneToMany, nameof(Resource.ActivityId), nameof(Id))]
    public List<Resource> Resources { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(RegistrationQuestion.ActivityId), nameof(Id))]
    public List<RegistrationQuestion> RegistrationQuestions { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ActivityRegistration.ActivityId), nameof(Id))]
    public List<ActivityRegistration> Registrations { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ActivityOrganizer.ActivityId), nameof(Id))]
    public List<ActivityOrganizer> Organizers { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(HackathonNotificationTemplate.ActivityId), nameof(Id))]
    public List<HackathonNotificationTemplate> NotificationTemplates { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(EventTimelineItem.ActivityId), nameof(Id))]
    public List<EventTimelineItem> TimelineItems { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ResourceRedemption.ActivityId), nameof(Id))]
    public List<ResourceRedemption> ResourceRedemptions { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(VenueCheckIn.ActivityId), nameof(Id))]
    public List<VenueCheckIn> VenueCheckIns { get; set; } = null!;

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

        Activity = new Activity { Id = activityId, Kind = ActivityKind.StandaloneWorkshop };
        return Activity;
    }
}
