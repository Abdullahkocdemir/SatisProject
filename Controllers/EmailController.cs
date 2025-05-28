using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Threading.Tasks; 
using MimeKit.Text; 

namespace SatışProject.Controllers
{
    public class EmailController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        public async Task<bool> SendGenericEmail(string senderName, string senderEmail, string senderAppPassword,
                                                 string receiverName, string receiverEmail, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(receiverName, receiverEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody 
                };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

                    await client.AuthenticateAsync(senderEmail, senderAppPassword);

                    await client.SendAsync(message);

                    await client.DisconnectAsync(true);
                }
                return true; 
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"E-posta gönderme hatası: {ex.Message}");
                return false; 
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendEmailFromForm(string receiverEmail, string subject, string body)
        {
            if (string.IsNullOrEmpty(receiverEmail) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
            {
                TempData["ErrorMessage"] = "Lütfen tüm alanları doldurun.";
                return RedirectToAction("Index");
            }

            // Kendi Gmail bilgileriniz
            string myGmailAddress = "kcdmirapo96@gmail.com";
            string myGmailAppPassword = "oauifwpqhjjgrzgn"; 
            string senderDisplayName = "KOÇDEMİR HIRDAVAT"; 

            bool success = await SendGenericEmail(
                senderDisplayName,
                myGmailAddress,
                myGmailAppPassword,
                "Değerli Müşterimiz", 
                receiverEmail,
                subject,
                body 
            );

            if (success)
            {
                TempData["SuccessMessage"] = "E-posta başarıyla gönderildi!";
            }
            else
            {
                TempData["ErrorMessage"] = "E-posta gönderilirken bir hata oluştu.";
            }

            return RedirectToAction("Index");
        }
    }
}