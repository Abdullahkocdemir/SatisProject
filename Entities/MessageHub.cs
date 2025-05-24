// SatışProject.Web/Hubs/MessageHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using SatışProject.Entities;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;

namespace SatışProject.Hubs
{
    public class MessageHub : Hub
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SatısContext _context;

        public MessageHub(UserManager<AppUser> userManager, SatısContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // NOT: SendMessage metodu artık doğrudan controller'dan çağrılmıyor.
        // Mesaj gönderme işlemi MessagesController'daki [HttpPost] Create metodu tarafından yapılıyor.
        // Bu metodu kaldırmak veya sadece test/debug amaçlı tutmak daha mantıklı.
        // Eğer bu metot hala bir yerden çağrılıyorsa ve database kaydı yapıyorsa,
        // MessagesController ile çakışmaya neden olur.

        // Aşağıdaki SendMessage metodunu kaldırman ya da sadece test amaçlı kullanman daha iyi olacaktır.
        // Eğer bir sohbet uygulaması gibi doğrudan Hub üzerinden mesaj gönderilecekse, bu metodun mantığı doğru.
        // Ama mevcut sistemde, controller üzerinden mesaj kaydı yapılıyor.

        // public async Task SendMessage(string recipientUserId, string subject, string content)
        // {
        //     // Bu kısım artık MessagesController'da yapılıyor.
        //     // Eğer bu Hub metodunu kullanmak istiyorsan, Controller'daki DB kayıt mantığını kaldır.
        //     // Aksi takdirde, bu metot sadece bildirim göndermeli veya tamamen kaldırılmalı.
        // }

        // Mesajı okundu olarak işaretle (bu metot doğru çalışıyor)
        public async Task MarkAsRead(int messageId)
        {
            var currentUser = await _userManager.GetUserAsync(Context.User);
            if (currentUser == null)
            {
                return;
            }

            var messageRecipient = await _context.MessageRecipients
                .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.RecipientUserId == currentUser.Id && !mr.IsRead);

            if (messageRecipient != null)
            {
                messageRecipient.IsRead = true;
                messageRecipient.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await Clients.User(currentUser.Id).SendAsync("UpdateUnreadCount"); // Sadece ilgili kullanıcıya gönder
            }
        }

        // Kullanıcı hub'a bağlandığında çalışır
        public override async Task OnConnectedAsync()
        {
            var currentUser = await _userManager.GetUserAsync(Context.User);
            if (currentUser != null)
            {
                Console.WriteLine($"Kullanıcı bağlandı: {currentUser.UserName} - ConnectionId: {Context.ConnectionId}");
                // Kullanıcı bağlandığında okunmamış mesaj sayısını güncellemesini tetikle
                await Clients.Caller.SendAsync("UpdateUnreadCount");
            }

            await base.OnConnectedAsync();
        }

        // Kullanıcı hub'dan ayrıldığında çalışır
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var currentUser = await _userManager.GetUserAsync(Context.User);
            if (currentUser != null)
            {
                Console.WriteLine($"Kullanıcı ayrıldı: {currentUser.UserName}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}