using IndiaGoldRates.Core.Enums;

namespace IndiaGoldRates.Core.Entities;

public class NotificationLog
{
    public Guid Id { get; set; }

    public Guid NotificationRuleId { get; set; }
    public NotificationRule NotificationRule { get; set; } = null!;

    public Guid UserId { get; set; }

    public NotificationTriggerType TriggerType { get; set; }
    public long? RateSnapshotId { get; set; }
    public decimal PriceInrPerGramAtSend { get; set; }
    public string EmailTo { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
