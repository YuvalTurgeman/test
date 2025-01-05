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
                Console.WriteLine($"SMTP Server: smtp.gmail.com");
                Console.WriteLine($"Port: 465");  // Using Gmail's SSL port
                Console.WriteLine($"Username: {_emailConfig.Username}");
                Console.WriteLine($"Password length: {_emailConfig.Password?.Length ?? 0}");
                
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
                        Console.WriteLine("Attempting to connect...");
                        await client.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                        Console.WriteLine("Connected successfully");

                        // Authenticate
                        Console.WriteLine("Attempting authentication...");
                        await client.AuthenticateAsync(_emailConfig.Username, _emailConfig.Password);
                        Console.WriteLine("Authenticated successfully");

                        // Send
                        Console.WriteLine("Attempting to send message...");
                        await client.SendAsync(message);
                        Console.WriteLine("Message sent successfully");
                    }
                    catch (AuthenticationException authEx)
                    {
                        Console.WriteLine($"Authentication failed: {authEx.Message}");
                        Console.WriteLine($"Inner exception: {authEx.InnerException?.Message}");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.GetType().Name} - {ex.Message}");
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
                Console.WriteLine($"Top level exception: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                throw;
            }
        }
    }
}