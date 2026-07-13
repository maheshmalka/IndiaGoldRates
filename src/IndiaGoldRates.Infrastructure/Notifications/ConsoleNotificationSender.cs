using IndiaGoldRates.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace IndiaGoldRates.Infrastructure.Notifications;

/// <summary>
/// Local-dev fallback used when Azure Communication Services isn't configured — logs the
/// would-be email instead of sending it, so notification-rule evaluation (digest due-check,
/// threshold crossing, cooldown) can be exercised end-to-end without a real ACS resource.
/// Program.cs only registers this when Acs:ConnectionString is empty; production always uses
/// AcsEmailNotificationSender.
/// </summary>
public class ConsoleNotificationSender(ILogger<ConsoleNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[DEV EMAIL — not actually sent, ACS not configured] To: {ToEmail} | Subject: {Subject}\n{Body}",
            message.ToEmail, message.Subject, message.Body);
        return Task.CompletedTask;
    }
}
