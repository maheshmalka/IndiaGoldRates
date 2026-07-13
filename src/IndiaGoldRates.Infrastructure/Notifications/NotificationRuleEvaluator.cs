using IndiaGoldRates.Core.Entities;
using IndiaGoldRates.Core.Enums;
using IndiaGoldRates.Core.Interfaces;
using IndiaGoldRates.Core.Models;
using IndiaGoldRates.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IndiaGoldRates.Infrastructure.Notifications;

public class NotificationRuleEvaluator(
    AppDbContext db,
    INotificationSender sender,
    ILogger<NotificationRuleEvaluator> logger) : INotificationRuleEvaluator
{
    private static readonly TimeSpan ThresholdCooldown = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan IstOffset = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30);

    public async Task EvaluateAsync(CurrentRatesView rates, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var activeRules = await db.NotificationRules
            .Include(r => r.User)
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var rule in activeRules)
        {
            var currentPrice = rates.GetPrice(rule.Metal, rule.Purity);
            if (currentPrice is null)
            {
                continue;
            }

            if (rule.DigestEnabled && IsDigestDue(rule, nowUtc))
            {
                await SendAsync(rule, NotificationTriggerType.Digest, currentPrice.Value, nowUtc, cancellationToken);
                rule.DigestLastSentAtUtc = nowUtc;
            }

            if (rule.ThresholdEnabled)
            {
                await EvaluateThresholdAsync(rule, currentPrice.Value, nowUtc, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EvaluateThresholdAsync(
        NotificationRule rule, decimal currentPrice, DateTime nowUtc, CancellationToken cancellationToken)
    {
        // Backfill the reference if it was never set (e.g. the rate cache was still empty when
        // the rule was created) — establish it silently rather than firing a spurious first alert.
        if (rule.ThresholdReferencePriceInrPerGram is null)
        {
            rule.ThresholdReferencePriceInrPerGram = currentPrice;
            rule.ThresholdReferenceSetAtUtc = nowUtc;
            return;
        }

        if (rule.ThresholdLastTriggeredAtUtc is { } lastTriggered && nowUtc - lastTriggered < ThresholdCooldown)
        {
            return;
        }

        var reference = rule.ThresholdReferencePriceInrPerGram.Value;
        var delta = Math.Abs(currentPrice - reference);
        var percentDelta = reference == 0 ? 0 : delta / reference * 100m;

        var crossedAbsolute = rule.ThresholdAbsoluteRupees is { } abs && delta >= abs;
        var crossedPercent = rule.ThresholdPercent is { } pct && percentDelta >= pct;

        if (!crossedAbsolute && !crossedPercent)
        {
            return;
        }

        await SendAsync(rule, NotificationTriggerType.Threshold, currentPrice, nowUtc, cancellationToken);
        rule.ThresholdLastTriggeredAtUtc = nowUtc;

        // Reset the reference to the price that just triggered — this is both the documented
        // semantics and the de-dupe mechanism: the bar moves, so oscillation near the old
        // threshold doesn't keep re-firing every cycle.
        rule.ThresholdReferencePriceInrPerGram = currentPrice;
        rule.ThresholdReferenceSetAtUtc = nowUtc;
    }

    private static bool IsDigestDue(NotificationRule rule, DateTime nowUtc)
    {
        if (rule.DigestFrequencyType == DigestFrequencyType.EveryNHours)
        {
            var interval = TimeSpan.FromHours(rule.DigestIntervalHours ?? 24);
            return rule.DigestLastSentAtUtc is null || nowUtc - rule.DigestLastSentAtUtc >= interval;
        }

        // DailyAtTime: fixed to Asia/Kolkata for all users in v1 (no per-user timezone field yet).
        var istNow = nowUtc + IstOffset;
        var alreadySentToday = rule.DigestLastSentAtUtc is { } last && (last + IstOffset).Date == istNow.Date;
        if (alreadySentToday)
        {
            return false;
        }

        var targetTime = rule.DigestTimeOfDay ?? new TimeOnly(9, 0);
        return TimeOnly.FromDateTime(istNow) >= targetTime;
    }

    private async Task SendAsync(
        NotificationRule rule, NotificationTriggerType triggerType, decimal price, DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var seriesLabel = SeriesLabel(rule);
        var (subject, body) = triggerType == NotificationTriggerType.Digest
            ? ($"{rule.City} {seriesLabel} — ₹{price:F2}/g",
               $"Your scheduled update: {seriesLabel} in {rule.City} is currently ₹{price:F2} per gram.")
            : ($"Alert: {rule.City} {seriesLabel} moved to ₹{price:F2}/g",
               $"{seriesLabel} in {rule.City} moved to ₹{price:F2} per gram " +
               $"(from a reference of ₹{rule.ThresholdReferencePriceInrPerGram:F2}).");

        var status = NotificationStatus.Sent;
        string? errorMessage = null;
        var toEmail = rule.User.Email ?? "";

        try
        {
            await sender.SendAsync(new NotificationMessage(toEmail, subject, body), cancellationToken);
        }
        catch (Exception ex)
        {
            status = NotificationStatus.Failed;
            errorMessage = ex.Message;
            logger.LogWarning(ex, "Failed to send {TriggerType} notification for rule {RuleId}.", triggerType, rule.Id);
        }

        db.NotificationLogs.Add(new NotificationLog
        {
            Id = Guid.NewGuid(),
            NotificationRuleId = rule.Id,
            UserId = rule.UserId,
            TriggerType = triggerType,
            PriceInrPerGramAtSend = price,
            EmailTo = toEmail,
            Status = status,
            ErrorMessage = errorMessage,
            SentAtUtc = nowUtc
        });
    }

    private static string SeriesLabel(NotificationRule rule) => (rule.Metal, rule.Purity) switch
    {
        (Metal.Gold, Purity.TwentyFourK) => "Gold 24K",
        (Metal.Gold, Purity.TwentyTwoK) => "Gold 22K",
        (Metal.Silver, _) => "Silver",
        _ => rule.Metal.ToString()
    };
}
