using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace hlasovanisvj.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}

public class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "noreply@localhost";
    public string FromName { get; set; } = "Hlasování SVJ";
}

/// <summary>Odesílá e-maily přes SMTP (konfigurace v sekci "Smtp").</summary>
public class SmtpEmailSender(SmtpOptions options) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(options.Host, options.Port, SecureSocketOptions.Auto);
        if (!string.IsNullOrEmpty(options.UserName))
            await client.AuthenticateAsync(options.UserName, options.Password);
        await client.SendAsync(new SendRequest(message));
        await client.DisconnectAsync(true);
    }
}

/// <summary>
/// Náhradní odesílání bez SMTP (vývoj): e-mail se jen zapíše do logu.
/// </summary>
public class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody)
    {
        logger.LogWarning("SMTP není nastaveno – e-mail se neodeslal. Komu: {To}, předmět: {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
