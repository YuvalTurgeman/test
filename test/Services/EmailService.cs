using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace test.Services
{
    public class EmailService
    {
        private readonly EmailConfiguration _emailConfig;

        public EmailService(IOptions<EmailConfiguration> emailConfig)
        {
            _emailConfig = emailConfig.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Print connection details for debugging
                Console.WriteLine("Attempting to send email with the following settings:");
                Console.WriteLine($"SMTP Server: {_emailConfig.SmtpServer}");
                Console.WriteLine($"Port: {_emailConfig.Port}");
                Console.WriteLine($"Username: {_emailConfig.Username}");
                Console.WriteLine($"To Email: {toEmail}");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailConfig.SenderName, _emailConfig.SenderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Configure client timeout
                    client.Timeout = 30000; // 30 seconds

                    try
                    {
                        // Connect using SSL
                        await client.ConnectAsync(_emailConfig.SmtpServer, _emailConfig.Port, SecureSocketOptions.Auto);
                        Console.WriteLine("Connected to SMTP server");

                        // Authenticate
                        await client.AuthenticateAsync(_emailConfig.Username, _emailConfig.Password);
                        Console.WriteLine("Authentication successful");

                        // Send
                        await client.SendAsync(message);
                        Console.WriteLine("Email sent successfully");
                    }
                    catch (AuthenticationException authEx)
                    {
                        Console.WriteLine($"Authentication failed: {authEx.Message}");
                        Console.WriteLine($"Inner exception: {authEx.InnerException?.Message}");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during email sending: {ex.GetType().Name} - {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        throw;
                    }
                    finally
                    {
                        if (client.IsConnected)
                        {
                            await client.DisconnectAsync(true);
                            Console.WriteLine("Disconnected from SMTP server");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Top level exception in SendEmailAsync: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}