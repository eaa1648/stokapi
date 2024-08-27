using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;

public class EmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly string _defaultFromAddress = "deneme.trying@yandex.com";
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, IEnumerable<IFormFile> attachments = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Mert Yazilim Staj", _defaultFromAddress));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body
        };

        if (attachments != null)
        {
            foreach (var attachment in attachments)
            {
                using (var stream = attachment.OpenReadStream())
                {
                    bodyBuilder.Attachments.Add(attachment.FileName, stream);
                }
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                await client.SendAsync(message);
                client.Disconnect(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            throw;
        }
    }
}
