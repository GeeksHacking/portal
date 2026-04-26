using GeeksHackingPortal.Api.Converters;
using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

[SugarIndex("IX_Hackathon_ShortCode", nameof(ShortCode), OrderByType.Asc)]
public class Hackathon
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(Id))]
    public Activity? Activity { get; set; } = null!;

    [SugarColumn(ColumnName = "Name", IsNullable = true)]
    public string? LegacyName { get; set; }

    [SugarColumn(ColumnName = "Description", IsNullable = true, ColumnDataType = "longtext")]
    public string? LegacyDescription { get; set; }

    [SugarColumn(ColumnName = "Venue", IsNullable = true)]
    public string? LegacyVenue { get; set; }

    [SugarColumn(ColumnDataType = "nvarchar(128)", SqlParameterDbType = typeof(UriConverter))]
    public Uri HomepageUri { get; set; } = null!;

    public string ShortCode { get; set; } = null!;

    [SugarColumn(ColumnName = "IsPublished")]
    public bool LegacyIsPublished { get; set; }

    [SugarColumn(ColumnName = "EventStartDate")]
    public DateTimeOffset LegacyEventStartDate { get; set; }

    [SugarColumn(ColumnName = "EventEndDate")]
    public DateTimeOffset LegacyEventEndDate { get; set; }

    public DateTimeOffset SubmissionsStartDate { get; set; }

    public DateTimeOffset ChallengeSelectionEndDate { get; set; }

    public DateTimeOffset SubmissionsEndDate { get; set; }

    public DateTimeOffset JudgingStartDate { get; set; }

    public DateTimeOffset JudgingEndDate { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(Participant.HackathonId))]
    public List<Participant> Participants { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(Organizer.HackathonId))]
    public List<Organizer> Organizers { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(Team.HackathonId), nameof(Id))]
    public List<Team> Teams { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(Resource.ActivityId), nameof(Id))]
    public List<Resource> Resources { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(Challenge.HackathonId))]
    public List<Challenge> Challenges { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ChallengeSubmission.HackathonId))]
    public List<ChallengeSubmission> Submissions { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(Judge.HackathonId))]
    public List<Judge> Judges { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(RegistrationQuestion.ActivityId), nameof(Id))]
    public List<RegistrationQuestion> RegistrationQuestions { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ActivityRegistration.ActivityId), nameof(Id))]
    public List<ActivityRegistration> ActivityRegistrations { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ActivityOrganizer.ActivityId), nameof(Id))]
    public List<ActivityOrganizer> ActivityOrganizers { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(HackathonNotificationTemplate.ActivityId), nameof(Id))]
    public List<HackathonNotificationTemplate> NotificationTemplates { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(EventTimelineItem.ActivityId), nameof(Id))]
    public List<EventTimelineItem> TimelineItems { get; set; } = null!;
}
