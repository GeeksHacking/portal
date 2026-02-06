using HackOMania.Api.Converters;
using SqlSugar;

namespace HackOMania.Api.Entities;

[SugarIndex("IX_Hackathon_ShortCode", nameof(ShortCode), OrderByType.Asc)]
public class Hackathon
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Venue { get; set; } = null!;

    [SugarColumn(ColumnDataType = "nvarchar(128)", SqlParameterDbType = typeof(UriConverter))]
    public Uri HomepageUri { get; set; } = null!;

    public string ShortCode { get; set; } = null!;

    /// <summary>
    /// When enabled, participants will be able to see and join the hackathon using the short code
    /// </summary>
    public bool IsPublished { get; set; }

    public DateTimeOffset EventStartDate { get; set; }

    public DateTimeOffset EventEndDate { get; set; }

    public DateTimeOffset SubmissionsStartDate { get; set; }

    public DateTimeOffset SubmissionsEndDate { get; set; }

    public DateTimeOffset JudgingStartDate { get; set; }

    public DateTimeOffset JudgingEndDate { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(Participant.HackathonId))]
    public List<Participant> Participants { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(Organizer.HackathonId))]
    public List<Organizer> Organizers { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(Team.HackathonId), nameof(Id))]
    public List<Team> Teams { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(Resource.HackathonId))]
    public List<Resource> Resources { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(Challenge.HackathonId))]
    public List<Challenge> Challenges { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(ChallengeSubmission.HackathonId))]
    public List<ChallengeSubmission> Submissions { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(Judge.HackathonId))]
    public List<Judge> Judges { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(RegistrationQuestion.HackathonId))]
    public List<RegistrationQuestion> RegistrationQuestions { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(HackathonNotificationTemplate.HackathonId))]
    public List<HackathonNotificationTemplate> NotificationTemplates { get; set; } = null!;
  
    [Navigate(NavigateType.OneToMany, nameof(EventTimelineItem.HackathonId))]
    public List<EventTimelineItem> TimelineItems { get; set; } = null!;
}
