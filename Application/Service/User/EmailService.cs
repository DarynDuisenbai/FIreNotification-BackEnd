using Application.Interfaces;
using Application.Settings;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;

namespace Application.Service.User
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendVerificationCodeAsync(string email, string code)
        {
            var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = "Код верификации",
                Body = $"Ваш код верификации: {code}. Код действителен 15 минут.",
                IsBodyHtml = false
            };

            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }
    }
}
