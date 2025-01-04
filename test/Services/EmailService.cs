namespace test.Services;
using MailKit.Net.Smtp;
using MimeKit;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");
        
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("DigiReads", emailSettings["Username"]));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        if (isHtml)
        {
            message.Body = new TextPart("html") { Text = body };
        }
        else
        {
            message.Body = new TextPart("plain") { Text = body };
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(emailSettings["Host"], int.Parse(emailSettings["Port"]), MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(emailSettings["Username"], emailSettings["Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
    //////test function
    public async Task TestEmailAsync()
    {
        await SendEmailAsync("test@example.com", "Test Subject", "<p>This is a test email.</p>");
    }
}