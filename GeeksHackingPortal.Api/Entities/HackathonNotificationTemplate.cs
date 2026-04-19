using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

[SugarIndex(
    "IX_HackathonNotificationTemplate_HackathonId_EventKey",
    nameof(HackathonId),
    OrderByType.Asc,
    nameof(EventKey),
    OrderByType.Asc
)]
public class HackathonNotificationTemplate
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid HackathonId { get; set; }

    [SugarColumn(ColumnDataType = "nvarchar(128)")]
    public string EventKey { get; set; } = null!;

    [SugarColumn(ColumnDataType = "nvarchar(256)")]
    public string TemplateId { get; set; } = null!;

    [Navigate(NavigateType.ManyToOne, nameof(HackathonId))]
    public Hackathon Hackathon { get; set; } = null!;
}
