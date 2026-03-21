using ScrivenerSync.Domain.Interfaces.Services;

namespace ScrivenerSync.Web.Services;

public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(
        string toEmail, string toName, string subject, string htmlBody,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] To: {ToName} <{ToEmail}> | Subject: {Subject}",
            toName, toEmail, subject);
        logger.LogDebug("[EMAIL BODY] {Body}", htmlBody);
        return Task.CompletedTask;
    }
}
