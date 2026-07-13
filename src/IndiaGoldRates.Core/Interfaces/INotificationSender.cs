namespace IndiaGoldRates.Core.Interfaces;

public record NotificationMessage(string ToEmail, string Subject, string Body);

/// <summary>
/// Abstraction over the delivery channel. v1 has a single Email implementation; kept as an
/// interface so WhatsApp/push/SMS can be added later without touching rule-evaluation logic.
/// </summary>
public interface INotificationSender
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}
