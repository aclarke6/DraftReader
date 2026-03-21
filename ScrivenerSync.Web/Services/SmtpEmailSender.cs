using System.Net;
using System.Net.Mail;
using ScrivenerSync.Domain.Interfaces.Services;

namespace ScrivenerSync.Web.Services;

public class SmtpEmailSender(EmailSettings settings, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(
        string toEmail, string toName, string subject, string htmlBody,
        CancellationToken ct = default)
    {
        using var client = new SmtpClient(settings.Smtp.Host, settings.Smtp.Port)
        {
            Credentials = new NetworkCredential(settings.Smtp.Username, settings.Smtp.Password),
            EnableSsl   = true
        };

        var message = new MailMessage
        {
            From       = new MailAddress(settings.Smtp.FromAddress, settings.Smtp.FromName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail, toName));

        await client.SendMailAsync(message, ct);
        logger.LogInformation("Email sent to {ToEmail} - {Subject}", toEmail, subject);
    }
}
