using IndiaGoldRates.Core.Enums;

namespace IndiaGoldRates.Core.Entities;

public class NotificationRule
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public City City { get; set; }
    public Metal Metal { get; set; }
    public Purity Purity { get; set; }
    public bool IsActive { get; set; } = true;

    // Digest
    public bool DigestEnabled { get; set; }
    public DigestFrequencyType DigestFrequencyType { get; set; }
    public TimeOnly? DigestTimeOfDay { get; set; }
    public int? DigestIntervalHours { get; set; }
    public DateTime? DigestLastSentAtUtc { get; set; }

    // Threshold (absolute rupees OR percent — either/both may be set; either crossing triggers)
    public bool ThresholdEnabled { get; set; }
    public decimal? ThresholdAbsoluteRupees { get; set; }
    public decimal? ThresholdPercent { get; set; }
    public decimal? ThresholdReferencePriceInrPerGram { get; set; }
    public DateTime? ThresholdReferenceSetAtUtc { get; set; }
    public DateTime? ThresholdLastTriggeredAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
}
