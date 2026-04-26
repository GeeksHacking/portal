using GeeksHackingPortal.Api.Converters;
using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

/// <summary>
/// Workshops/events that are organized independent of a <see cref="Hackathon"/>.
///
/// The term "event" is avoided to avoid conflict with language / runtime features.
/// </summary>
[SugarIndex("IX_StandaloneWorkshop_ShortCode", nameof(ShortCode), OrderByType.Asc, IsUnique = true)]
public class StandaloneWorkshop
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(Id))]
    public Activity Activity { get; set; } = null!;

    [SugarColumn(ColumnDataType = "nvarchar(128)", SqlParameterDbType = typeof(UriConverter))]
    public Uri HomepageUri { get; set; } = null!;

    public string ShortCode { get; set; } = null!;

    public int MaxParticipants { get; set; }

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
}
