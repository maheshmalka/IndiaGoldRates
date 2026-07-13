using Azure.Communication.Email;
using IndiaGoldRates.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IndiaGoldRates.Infrastructure.Notifications;

/// <summary>Sends notification emails via Azure Communication Services Email.</summary>
public class AcsEmailNotificationSender(IConfiguration configuration, ILogger<AcsEmailNotificationSender> logger)
    : INotificationSender
{
    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        var connectionString = configuration["Acs:ConnectionString"]
            ?? throw new InvalidOperationException("Acs:ConnectionString is not configured.");
        var senderAddress = configuration["Acs:SenderAddress"]
            ?? throw new InvalidOperationException("Acs:SenderAddress is not configured.");

        var client = new EmailClient(connectionString);
        var emailMessage = new EmailMessage(
            senderAddress,
            message.ToEmail,
            new EmailContent(message.Subject) { PlainText = message.Body });

        var operation = await client.SendAsync(Azure.WaitUntil.Started, emailMessage, cancellationToken);
        logger.LogInformation("Email queued via ACS (operation {OperationId}) to {ToEmail}.", operation.Id, message.ToEmail);
    }
}
