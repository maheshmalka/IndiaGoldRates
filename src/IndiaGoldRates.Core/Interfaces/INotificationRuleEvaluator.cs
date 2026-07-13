using IndiaGoldRates.Core.Models;

namespace IndiaGoldRates.Core.Interfaces;

/// <summary>
/// Evaluates all active notification rules against the latest rates each poll cycle
/// (digest due-check, threshold crossing) and dispatches via INotificationSender.
/// </summary>
public interface INotificationRuleEvaluator
{
    Task EvaluateAsync(CurrentRatesView rates, DateTime nowUtc, CancellationToken cancellationToken);
}
