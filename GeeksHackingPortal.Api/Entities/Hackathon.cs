using GeeksHackingPortal.Api.Converters;
using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

[SugarIndex("IX_Hackathon_ShortCode", nameof(ShortCode), OrderByType.Asc)]
public class Hackathon
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

    [SugarColumn(ColumnName = "Name", IsNullable = true)]
    public string? LegacyName { get; set; }

    [SugarColumn(IsIgnore = true)]
    public string Name
    {
        get => Activity?.Title ?? LegacyName ?? string.Empty;
        set
        {
            EnsureActivity().Title = value;
            LegacyName = value;
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

    [SugarColumn(ColumnName = "Venue", IsNullable = true)]
    public string? LegacyVenue { get; set; }

    [SugarColumn(IsIgnore = true)]
    public string Venue
    {
        get => Activity?.Location ?? LegacyVenue ?? string.Empty;
        set
        {
            EnsureActivity().Location = value;
            LegacyVenue = value;
        }
    }

    [SugarColumn(ColumnDataType = "nvarchar(128)", SqlParameterDbType = typeof(UriConverter))]
    public Uri HomepageUri { get; set; } = null!;

    public string ShortCode { get; set; } = null!;

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

    [SugarColumn(ColumnName = "EventStartDate")]
    public DateTimeOffset LegacyEventStartDate { get; set; }

    [SugarColumn(IsIgnore = true)]
    public DateTimeOffset EventStartDate
    {
        get => Activity?.StartTime ?? LegacyEventStartDate;
        set
        {
            EnsureActivity().StartTime = value;
            LegacyEventStartDate = value;
        }
    }

    [SugarColumn(ColumnName = "EventEndDate")]
    public DateTimeOffset LegacyEventEndDate { get; set; }

    [SugarColumn(IsIgnore = true)]
    public DateTimeOffset EventEndDate
    {
        get => Activity?.EndTime ?? LegacyEventEndDate;
        set
        {
            EnsureActivity().EndTime = value;
            LegacyEventEndDate = value;
        }
    }

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
            Kind = ActivityKind.Hackathon,
            Title = LegacyName ?? string.Empty,
            Description = LegacyDescription ?? string.Empty,
            Location = LegacyVenue ?? string.Empty,
            StartTime = LegacyEventStartDate,
            EndTime = LegacyEventEndDate,
            IsPublished = LegacyIsPublished,
        };
        return Activity;
    }
}
