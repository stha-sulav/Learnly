using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;
using System.Diagnostics; // For Debug.WriteLine

namespace Learnly.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // For development purposes, log the email to the console or debug output.
            // In a real application, you would integrate with an actual email sending service (e.g., SendGrid, Mailgun, SMTP).
            Debug.WriteLine($"To: {email}");
            Debug.WriteLine($"Subject: {subject}");
            Debug.WriteLine($"Message: {htmlMessage}");

            // You can also use Console.WriteLine for console applications or if you're running Kestrel directly
            Console.WriteLine($"Sending email to {email} with subject: {subject}");
            Console.WriteLine($"Message: {htmlMessage}");

            return Task.CompletedTask;
        }
    }
}
