using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Threading.Tasks; // Asenkron işlemler için
using MimeKit.Text; // HtmlBody için

namespace SatışProject.Controllers
{
    public class EmailController : Controller
    {
        // E-posta gönderme formunu gösterecek sayfa
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
                    HtmlBody = htmlBody // HTML içeriği kullanıyoruz
                };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Gmail SMTP sunucusuna bağlanma
                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

                    // Kimlik doğrulama (Gmail kullanıcı adı ve Uygulama Şifresi)
                    await client.AuthenticateAsync(senderEmail, senderAppPassword);

                    // E-postayı gönderme
                    await client.SendAsync(message);

                    // Bağlantıyı kapatma
                    await client.DisconnectAsync(true);
                }
                return true; // E-posta başarıyla gönderildi
            }
            catch (System.Exception ex)
            {
                // Hata detaylarını loglayabilirsiniz. (örn: Serilog, NLog)
                Console.WriteLine($"E-posta gönderme hatası: {ex.Message}");
                // Log the full exception for debugging: Console.WriteLine(ex.ToString());
                return false; // E-posta gönderme başarısız oldu
            }
        }


        // Bu metot, SendGenericEmail metodunu kullanarak harici bir istekten e-posta göndermek içindir.
        // Genellikle, bu tür bir metodu doğrudan bir form POST'u için kullanırsınız.
        [HttpPost]
        public async Task<IActionResult> SendEmailFromForm(string receiverEmail, string subject, string body)
        {
            // Bu kısımda validation (doğrulama) ekleyebilirsiniz.
            if (string.IsNullOrEmpty(receiverEmail) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
            {
                TempData["ErrorMessage"] = "Lütfen tüm alanları doldurun.";
                return RedirectToAction("Index");
            }

            // Kendi Gmail bilgileriniz
            string myGmailAddress = "kcdmirapo96@gmail.com";
            string myGmailAppPassword = "oauifwpqhjjgrzgn"; // **ÖNEMLİ: Kendi Gmail Uygulama Şifreniz**
            string senderDisplayName = "KOÇDEMİR HIRDAVAT"; // Gönderen görünen adı

            bool success = await SendGenericEmail(
                senderDisplayName,
                myGmailAddress,
                myGmailAppPassword,
                "Değerli Müşterimiz", // Alıcı adını modelden almanız gerekebilir. Şimdilik sabit bir değer verdim.
                receiverEmail,
                subject,
                body // Bu metot doğrudan formdan gelen body'yi kullanır, dilerseniz HTML formatına çevirebilirsiniz.
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