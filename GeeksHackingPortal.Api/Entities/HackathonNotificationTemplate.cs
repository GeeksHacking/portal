using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

[SugarIndex(
    "IX_HackathonNotificationTemplate_ActivityId_EventKey",
    nameof(ActivityId),
    OrderByType.Asc,
    nameof(EventKey),
    OrderByType.Asc
)]
public class HackathonNotificationTemplate
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [SugarColumn(OldColumnName = "HackathonId")]
    public Guid ActivityId { get; set; }

    [SugarColumn(ColumnDataType = "nvarchar(128)")]
    public string EventKey { get; set; } = null!;

    [SugarColumn(ColumnDataType = "nvarchar(256)")]
    public string TemplateId { get; set; } = null!;

    [Navigate(NavigateType.ManyToOne, nameof(ActivityId))]
    public Activity Activity { get; set; } = null!;
}
