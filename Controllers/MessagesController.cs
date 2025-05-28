using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;
using Microsoft.AspNetCore.SignalR; 
using SatışProject.Models;
using SatışProject.Hubs; 

namespace SatışProject.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly SatısContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<MessageHub> _hubContext;

        public MessagesController(SatısContext context, UserManager<AppUser> userManager, IHubContext<MessageHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        // Gelen Kutusu
        public async Task<IActionResult> Inbox()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var inboxMessages = await _context.MessageRecipients
                .Where(mr => mr.RecipientUserId == currentUser.Id)
                .Include(mr => mr.Message)
                    .ThenInclude(m => m.Sender)
                .OrderByDescending(mr => mr.Message.SentAt)
                .ToListAsync();

            var model = inboxMessages.Select(mr => new MessageViewModel
            {
                MessageId = mr.MessageId,
                SenderFullName = mr.Message.Sender.FullName,
                SenderUserName = mr.Message.Sender.UserName!,
                Subject = mr.Message.Subject,
                Content = mr.Message.Content,
                SentAt = mr.Message.SentAt,
                IsRead = mr.IsRead
            }).ToList();

            return View(model);
        }

        // Giden Kutusu
        public async Task<IActionResult> SentItems()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var sentMessages = await _context.Messages
                .Where(m => m.SenderUserId == currentUser.Id)
                .Include(m => m.Recipients)
                    .ThenInclude(r => r.Recipient)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var model = sentMessages.Select(m => new MessageViewModel
            {
                MessageId = m.MessageId,
                SenderFullName = m.Sender.FullName,
                SenderUserName = m.Sender.UserName!,
                Subject = m.Subject,
                Content = m.Content,
                SentAt = m.SentAt,
                Recipients = m.Recipients.Select(r => new MessageRecipientViewModel
                {
                    RecipientFullName = r.Recipient.FullName,
                    RecipientUserName = r.Recipient.UserName!,
                    IsRead = r.IsRead
                }).ToList()
            }).ToList();

            return View(model);
        }

        // Mesaj Detayı
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var messageRecipient = await _context.MessageRecipients
                .Include(mr => mr.Message)
                    .ThenInclude(m => m.Sender)
                .Include(mr => mr.Recipient)
                .FirstOrDefaultAsync(mr => mr.MessageId == id && mr.RecipientUserId == currentUser.Id);

            if (messageRecipient == null)
            {
                var sentMessage = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipients)
                        .ThenInclude(r => r.Recipient)
                    .FirstOrDefaultAsync(m => m.MessageId == id && m.SenderUserId == currentUser.Id);

                if (sentMessage == null)
                {
                    return NotFound();
                }

                var sentMessageDetail = new MessageViewModel
                {
                    MessageId = sentMessage.MessageId,
                    SenderFullName = sentMessage.Sender.FullName,
                    SenderUserName = sentMessage.Sender.UserName!,
                    Subject = sentMessage.Subject,
                    Content = sentMessage.Content,
                    SentAt = sentMessage.SentAt,
                    IsRead = true,
                    Recipients = sentMessage.Recipients.Select(r => new MessageRecipientViewModel
                    {
                        RecipientFullName = r.Recipient.FullName,
                        RecipientUserName = r.Recipient.UserName!,
                        IsRead = r.IsRead
                    }).ToList()
                };
                return View(sentMessageDetail);
            }

            if (!messageRecipient.IsRead)
            {
                messageRecipient.IsRead = true;
                messageRecipient.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            var model = new MessageViewModel
            {
                MessageId = messageRecipient.MessageId,
                SenderFullName = messageRecipient.Message.Sender.FullName,
                SenderUserName = messageRecipient.Message.Sender.UserName!,
                Subject = messageRecipient.Message.Subject,
                Content = messageRecipient.Message.Content,
                SentAt = messageRecipient.Message.SentAt,
                IsRead = messageRecipient.IsRead
            };

            return View(model);
        }

        // Yeni Mesaj Oluşturma Formu
        public async Task<IActionResult> Create()
        {
            var users = await _userManager.Users.ToListAsync();
            ViewBag.Users = users.Select(u => new { u.Id, FullName = u.FullName ?? u.UserName }).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SendMessageViewModel model)
        {
            if (ModelState.IsValid)
            {
                var sender = await _userManager.GetUserAsync(User);
                if (sender == null)
                {
                    ModelState.AddModelError("", "Mesaj göndermek için giriş yapmalısınız.");
                    var users = await _userManager.Users.ToListAsync();
                    ViewBag.Users = users.Select(u => new { u.Id, FullName = u.FullName ?? u.UserName }).ToList();
                    return View(model);
                }

                try
                {
                    // Mesajı veritabanına kaydet
                    var message = new Message
                    {
                        SenderUserId = sender.Id,
                        Subject = model.Subject,
                        Content = model.Content,
                        SentAt = DateTime.Now
                    };

                    _context.Messages.Add(message);
                    await _context.SaveChangesAsync();

                    var messageRecipient = new MessageRecipient
                    {
                        MessageId = message.MessageId,
                        RecipientUserId = model.RecipientUserId,
                        IsRead = false
                    };

                    _context.MessageRecipients.Add(messageRecipient);
                    await _context.SaveChangesAsync();

                    // Alıcıya SignalR üzerinden bildirim gönder
                    await _hubContext.Clients.User(model.RecipientUserId).SendAsync(
                        "ReceiveNotification",
                        "Yeni Mesaj",
                        $"{sender.FullName ?? sender.UserName} size bir mesaj gönderdi.",
                        "info"
                    );

                    TempData["SuccessMessage"] = "Mesajınız başarıyla gönderildi!";
                    return RedirectToAction(nameof(SentItems));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Mesaj gönderilirken bir hata oluştu: {ex.Message}");
                    Console.Error.WriteLine($"Mesaj gönderme hatası: {ex}");
                }
            }

            var allUsers = await _userManager.Users.ToListAsync();
            ViewBag.Users = allUsers.Select(u => new { u.Id, FullName = u.FullName ?? u.UserName }).ToList();
            return View(model);
        }

        // Okunmamış mesaj sayısını döndüren AJAX endpoint'i
        [HttpGet]
        public async Task<IActionResult> GetUnreadMessageCount()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(0);
            }

            var unreadCount = await _context.MessageRecipients
                .CountAsync(mr => mr.RecipientUserId == currentUser.Id && !mr.IsRead);

            return Json(unreadCount);
        }
    }
}