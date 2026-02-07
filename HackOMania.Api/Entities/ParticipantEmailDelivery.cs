using SqlSugar;

namespace HackOMania.Api.Entities;

[SugarIndex(
    "IX_ParticipantEmailDelivery_ParticipantId_SentAt",
    nameof(ParticipantId),
    OrderByType.Asc,
    nameof(SentAt),
    OrderByType.Desc
)]
[SugarIndex(
    "IX_ParticipantEmailDelivery_HackathonId_UserId",
    nameof(HackathonId),
    OrderByType.Asc,
    nameof(UserId),
    OrderByType.Asc
)]
public class ParticipantEmailDelivery
{
    public enum EmailDeliveryStatus
    {
        Sent = 1,
        Failed = 2,
        Skipped = 3,
    }

    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    public Guid HackathonId { get; set; }

    public Guid ParticipantId { get; set; }

    public Guid UserId { get; set; }

    [SugarColumn(ColumnDataType = "nvarchar(320)")]
    public string ToEmail { get; set; } = null!;

    [SugarColumn(ColumnDataType = "nvarchar(128)")]
    public string EventKey { get; set; } = null!;

    [SugarColumn(ColumnDataType = "nvarchar(256)")]
    public string TemplateId { get; set; } = null!;

    [SugarColumn(ColumnDataType = "nvarchar(64)")]
    public string Provider { get; set; } = null!;

    [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(128)")]
    public string? ProviderMessageId { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(1024)")]
    public string? ErrorMessage { get; set; }

    public EmailDeliveryStatus Status { get; set; }

    public DateTimeOffset SentAt { get; set; }

    [Navigate(NavigateType.ManyToOne, nameof(ParticipantId))]
    public Participant Participant { get; set; } = null!;
}
