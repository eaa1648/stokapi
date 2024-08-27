using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MiniExcelLibs;
using Npgsql;
using Microsoft.Extensions.Configuration;

public class UserStockEmailService
{
    private readonly string _connectionString;
    private readonly SmtpSettings _smtpSettings;

    public UserStockEmailService(Microsoft.Extensions.Configuration.IConfiguration configuration, IOptions<SmtpSettings> smtpSettings)
    {
        _connectionString = configuration.GetConnectionString("PostgreSql");
        _smtpSettings = smtpSettings.Value;
    }

    public async Task SendUserStocksEmail()
    {
        var users = GetUsers();

        foreach (var user in users)
        {
            if (string.IsNullOrEmpty(user.Email))
            {
                // Log or handle the situation where the user's email is missing
                continue;
            }

            var userStocks = GetUserStocks(user.Username);
            var excelFile = GenerateExcelFile(userStocks);

            await SendEmail(user.Email, excelFile);
        }
    }

    private List<UserEmailDto> GetUsers()
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            var sql = "SELECT username, email FROM users";
            return connection.Query<UserEmailDto>(sql).AsList();
        }
    }

    private List<UserStock> GetUserStocks(string username)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            var sql = "SELECT stockname, quantity, purchaseprice, purchasedate FROM userstocks WHERE username = @username";
            return connection.Query<UserStock>(sql, new { username }).AsList();
        }
    }

    private byte[] GenerateExcelFile(List<UserStock> userStocks)
    {
        using (var stream = new MemoryStream())
        {
            MiniExcel.SaveAs(stream, userStocks);
            return stream.ToArray();
        }
    }

    private async Task SendEmail(string email, byte[] excelFile)
    {
        if (string.IsNullOrEmpty(email))
        {
            throw new ArgumentNullException(nameof(email), "Email address cannot be null or empty.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Mert Yazilim Staj", _smtpSettings.Username));
        message.To.Add(new MailboxAddress("", email)); // Email adresi burada kullanılıyor
        message.Subject = "Your Stock Portfolio";

        var body = new TextPart("plain")
        {
            Text = "Attached is your stock portfolio report."
        };

        var attachment = new MimePart("application", "vnd.ms-excel")
        {
            Content = new MimeContent(new MemoryStream(excelFile), ContentEncoding.Default),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
            FileName = "UserStocks.xlsx"
        };

        var multipart = new Multipart("mixed");
        multipart.Add(body);
        multipart.Add(attachment);

        message.Body = multipart;

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
