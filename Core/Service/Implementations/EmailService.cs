using Service.Abstraction.Contracts;
using Shared.DTOs.IdentityModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations {
    public class EmailService : IEmailService {
        public async Task SendEmail(Email email) {

            // 1. Configure SMTP Client
            using var client = new SmtpClient("smtp.gmail.com", 587) {
                EnableSsl = true,
                Credentials = new NetworkCredential("Email", "PassWord")
            };

            // 2. Create Mail Message
            using var mailMessage = new MailMessage {
                From = new MailAddress("Email", "Legal AI System"),
                Subject = email.Subject,
                Body = email.Body,
            };

            // 3. Add Receiver
            mailMessage.To.Add(email.To);

            // 4. Send Email
            await client.SendMailAsync(mailMessage);
        }
    }
}
