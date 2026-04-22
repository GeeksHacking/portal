using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

public class Activity
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public ActivityKind Kind { get; set; }

    public string Title { get; set; } = null!;

    [SugarColumn(ColumnDataType = "longtext")]
    public string Description { get; set; } = null!;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public string Location { get; set; } = null!;

    public bool IsPublished { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Navigate(NavigateType.OneToMany, nameof(Resource.ActivityId))]
    public List<Resource> Resources { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(RegistrationQuestion.ActivityId))]
    public List<RegistrationQuestion> RegistrationQuestions { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ActivityRegistration.ActivityId))]
    public List<ActivityRegistration> Registrations { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ActivityOrganizer.ActivityId))]
    public List<ActivityOrganizer> Organizers { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(HackathonNotificationTemplate.ActivityId))]
    public List<HackathonNotificationTemplate> NotificationTemplates { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(EventTimelineItem.ActivityId))]
    public List<EventTimelineItem> TimelineItems { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ResourceRedemption.ActivityId))]
    public List<ResourceRedemption> ResourceRedemptions { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(VenueCheckIn.ActivityId))]
    public List<VenueCheckIn> VenueCheckIns { get; set; } = null!;
}

public enum ActivityKind
{
    Hackathon = 1,
    HackathonWorkshop = 2,
    StandaloneWorkshop = 3,
}
