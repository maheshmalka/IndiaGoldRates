using IndiaGoldRates.Core.Enums;

namespace IndiaGoldRates.Api.Contracts;

public record NotificationRuleDto(
    Guid Id,
    City City,
    Metal Metal,
    Purity Purity,
    bool IsActive,
    bool DigestEnabled,
    DigestFrequencyType DigestFrequencyType,
    TimeOnly? DigestTimeOfDay,
    int? DigestIntervalHours,
    DateTime? DigestLastSentAtUtc,
    bool ThresholdEnabled,
    decimal? ThresholdAbsoluteRupees,
    decimal? ThresholdPercent,
    decimal? ThresholdReferencePriceInrPerGram,
    DateTime? ThresholdReferenceSetAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record UpsertNotificationRuleRequest(
    City City,
    Metal Metal,
    Purity Purity,
    bool IsActive,
    bool DigestEnabled,
    DigestFrequencyType DigestFrequencyType,
    TimeOnly? DigestTimeOfDay,
    int? DigestIntervalHours,
    bool ThresholdEnabled,
    decimal? ThresholdAbsoluteRupees,
    decimal? ThresholdPercent);
