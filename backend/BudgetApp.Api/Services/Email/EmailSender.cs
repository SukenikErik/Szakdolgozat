using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BudgetApp.Api.Services.Email;

public record EmailMessage(string To, string Subject, string TextBody, string HtmlBody);

/// <summary>An interface, so the tests can replace the real SMTP sending.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}

public class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message)
    {
        var mime = new MimeMessage
        {
            Subject = message.Subject,
            Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody()
        };
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mime.To.Add(new MailboxAddress(string.Empty, message.To));

        // The user waits for the answer of the request, so an unreachable server must not block it for long.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var client = new SmtpClient();

        // Port 465 is encrypted from the start, the other ports must switch to TLS before anything is sent.
        var security = !_options.UseTls ? SecureSocketOptions.None
            : _options.Port == 465 ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await client.ConnectAsync(_options.Host, _options.Port, security, timeout.Token);
        if (!string.IsNullOrEmpty(_options.UserName))
        {
            await client.AuthenticateAsync(_options.UserName, _options.Password ?? string.Empty, timeout.Token);
        }

        await client.SendAsync(new SendRequest(mime), timeout.Token);
        await client.DisconnectAsync(quit: true, timeout.Token);
    }
}
