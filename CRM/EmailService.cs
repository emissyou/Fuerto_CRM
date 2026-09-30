using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace CRM_DesignServices.winforms;

/// <summary>
/// Service responsible for dispatching real emails across SMTP (Gmail, Outlook, custom).
/// Handles SSL, credentials sanitization, connection timeouts, and user-friendly error diagnostics.
/// </summary>
public static class EmailService
{
    public static async Task<(bool Success, string Message)> SendEmailAsync(
        string recipientEmail,
        string subject,
        string body,
        string? recipientName = null,
        bool isHtml = false)
    {
        var settings = EmailSettings.Current;

        if (!settings.IsConfigured)
        {
            return (false, "EMAIL_NOT_CONFIGURED: Please configure your sender Gmail address and Google App Password.");
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return (false, "Recipient email address cannot be empty.");
        }

        try
        {
            // Sanitize password (Google App passwords often have spaces like 'abcd efgh ijkl mnop')
            string cleanPassword = settings.SenderPassword.Replace(" ", "").Trim();

            using var message = new MailMessage();
            message.From = new MailAddress(settings.SenderEmail.Trim(), settings.SenderDisplayName);
            message.To.Add(string.IsNullOrWhiteSpace(recipientName) 
                ? new MailAddress(recipientEmail.Trim()) 
                : new MailAddress(recipientEmail.Trim(), recipientName.Trim()));

            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;
            message.SubjectEncoding = Encoding.UTF8;
            message.BodyEncoding = Encoding.UTF8;

            using var smtp = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                EnableSsl = settings.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(settings.SenderEmail.Trim(), cleanPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20000 // 20s timeout
            };

            await smtp.SendMailAsync(message);
            return (true, $"Email successfully delivered to {recipientEmail} via {settings.SmtpHost}!");
        }
        catch (SmtpException ex)
        {
            if (ex.Message.Contains("5.7.8") || 
                ex.Message.Contains("Authentication") || 
                ex.Message.Contains("Username and Password not accepted") ||
                ex.StatusCode == SmtpStatusCode.GeneralFailure)
            {
                return (false, 
                    "Gmail Authentication Failed: Google rejected the login.\n\n" +
                    "To fix this:\n" +
                    "1. Ensure you are using a 16-character 'Google App Password', NOT your standard Gmail password.\n" +
                    "2. To generate one: Go to myaccount.google.com -> Security -> 2-Step Verification -> App Passwords.\n" +
                    "3. Enter the 16-character code in CRM Email Settings.");
            }

            return (false, $"SMTP Transmission Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to send email: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a self-test email to verify the configured credentials before saving.
    /// </summary>
    public static async Task<(bool Success, string Message)> SendTestEmailAsync(EmailSettings testSettings, string targetEmail)
    {
        try
        {
            string cleanPassword = testSettings.SenderPassword.Replace(" ", "").Trim();

            using var message = new MailMessage();
            message.From = new MailAddress(testSettings.SenderEmail.Trim(), testSettings.SenderDisplayName);
            message.To.Add(new MailAddress(targetEmail.Trim()));
            message.Subject = "✅ Fuerto CRM — Email Connection Test";
            message.Body = 
                $"Hello,\n\n" +
                $"This is a test email confirming that your CRM Email Configuration is working correctly!\n\n" +
                $"• SMTP Server: {testSettings.SmtpHost}:{testSettings.SmtpPort}\n" +
                $"• Sender: {testSettings.SenderEmail}\n" +
                $"• Delivered At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                $"You are now ready to send real retention offers and notifications to your customers.\n\n" +
                $"Best regards,\n{testSettings.SenderDisplayName}";

            message.IsBodyHtml = false;

            using var smtp = new SmtpClient(testSettings.SmtpHost, testSettings.SmtpPort)
            {
                EnableSsl = testSettings.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(testSettings.SenderEmail.Trim(), cleanPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000
            };

            await smtp.SendMailAsync(message);
            return (true, $"Test email successfully delivered to {targetEmail}!");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
