using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PatientTracker.Application.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
            var smtpUsername = _configuration["Email:SmtpUsername"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["Email:FromName"] ?? "Patient Tracker";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogError("SMTP configuration is missing. Please check appsettings.json");
                throw new InvalidOperationException("Email service is not properly configured");
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Email}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetLink)
    {
        var subject = "Reset your Patient Tracker password";

        var body = $@"
    <!DOCTYPE html>
    <html lang='en'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>Password Reset</title>
    </head>
    <body style='margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased;'>
        <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; padding: 40px 20px;'>
            <tr>
                <align='center' valign='top'>
                    <table width='100%' max-width='560' border='0' cellspacing='0' cellpadding='0' style='max-width: 560px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -1px rgba(0, 0, 0, 0.03); border: 1px border-color: #e2e8f0;'>
                        
                        <tr>
                            <td style='background-color: #2563eb; height: 6px;'></td>
                        </tr>

                        <tr>
                            <td style='padding: 40px 32px;'>
                                <h1 style='margin: 0 0 24px 0; color: #1e3a8a; font-size: 22px; font-weight: 700; tracking: -0.02em;'>
                                    Patient Tracker
                                </h1>
                                
                                <h2 style='margin: 0 0 16px 0; color: #0f172a; font-size: 18px; font-weight: 600;'>
                                    Password Reset Request
                                </h2>
                                
                                <p style='margin: 0 0 12px 0; color: #475569; font-size: 15px; line-height: 24px;'>
                                    Hello,
                                </p>
                                
                                <p style='margin: 0 0 24px 0; color: #475569; font-size: 15px; line-height: 24px;'>
                                    We received a request to reset the password associated with your Patient Tracker account. Click the button below to secure your account and set up a new password:
                                </p>
                                
                                <table border='0' cellspacing='0' cellpadding='0' style='margin: 32px 0;'>
                                    <tr>
                                        <td align='center' style='border-radius: 8px; background-color: #2563eb;'>
                                            <a href='{resetLink}' target='_blank' style='display: inline-block; padding: 14px 28px; font-size: 15px; font-weight: 600; color: #ffffff; text-decoration: none; border-radius: 8px; border: 1px solid #2563eb;'>
                                                Reset Password
                                            </a>
                                        </td>
                                    </tr>
                                </table>

                                <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f1f5f9; border-radius: 8px; margin-bottom: 24px;'>
                                    <tr>
                                        <td style='padding: 12px 16px; color: #64748b; font-size: 13px; line-height: 20px;'>
                                            ℹ️ <strong>Security note:</strong> This setup link is temporary and will automatically expire in <strong>1 hour</strong>.
                                        </td>
                                    </tr>
                                </table>
                                
                                <p style='margin: 0 0 32px 0; color: #475569; font-size: 14px; line-height: 22px;'>
                                    If you didn't ask for this change, you can safely disregard this message. Your password will remain completely secure.
                                </p>
                                
                                <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 0 0 24px 0;' />
                                
                                <p style='margin: 0; color: #94a3b8; font-size: 13px; line-height: 20px;'>
                                    Warm regards,<br/>
                                    <strong style='color: #475569;'>The Patient Tracker Team</strong>
                                </p>
                            </td>
                        </tr>
                        
                        <tr>
                            <td style='background-color: #f8fafc; padding: 24px 32px; border-top: 1px solid #e2e8f0; text-align: center;'>
                                <p style='margin: 0; color: #94a3b8; font-size: 12px; line-height: 18px;'>
                                    This is an automated operational notification. Please do not reply directly to this inbox.
                                </p>
                            </td>
                        </tr>
                    </table>
                </align>
            </tr>
        </table>
    </body>
    </html>";

        await SendEmailAsync(to, subject, body);
    }
}
