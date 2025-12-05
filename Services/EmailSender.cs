using Microsoft.AspNetCore.Identity.UI.Services;

namespace COMP2139_Assignment1_1.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // For development: just log to console
            Console.WriteLine($"\n📧 Email Sent To: {email}");
            Console.WriteLine($"📋 Subject: {subject}");
            Console.WriteLine($"📄 Message: {htmlMessage}\n");
            
            return Task.CompletedTask;
            
            // TODO: For production, integrate SendGrid or SMTP
        }
    }
}