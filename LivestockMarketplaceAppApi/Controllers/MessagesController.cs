using LivestockMarketplaceApp.Data;
using LivestockMarketplaceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LivestockMarketplaceApp.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessagesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. جلب قائمة جهات الاتصال (GET: api/MessagesApi/Contacts)
        [HttpGet("Contacts")]
        public async Task<IActionResult> GetContacts()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            int currentUserId = int.Parse(userIdClaim);

            // 🔥 جلب كل المحادثات التي يشارك فيها المستخدم
            var allMessages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .ToListAsync(); // نجلبها للذاكرة لتسهيل الترتيب والتجميع

            // 🔥 تجميعها حسب الشخص الآخر وجلب آخر رسالة
            var contacts = allMessages
                .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Select(g =>
                {
                    // الترتيب من الأحدث للأقدم وأخذ أول رسالة (وهي الأحدث)
                    var lastMsg = g.OrderByDescending(m => m.SentAt).First();
                    var contactUser = lastMsg.SenderId == currentUserId ? lastMsg.Receiver : lastMsg.Sender;

                    return new
                    {
                        ContactId = g.Key,
                        ContactName = contactUser?.FullName ?? "مستخدم",
                        LastMessage = lastMsg.Content // ✨ إضافة آخر رسالة هنا لتطبيق الجوال
                    };
                })
                .ToList();

            return Ok(new { success = true, data = contacts });
        }

        // 2. جلب تاريخ المحادثة مع شخص محدد (GET: api/MessagesApi/Chat/{receiverId})
        [HttpGet("Chat/{receiverId}")]
        public async Task<IActionResult> GetChatHistory(int receiverId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            int currentUserId = int.Parse(userIdClaim);

            var chatHistory = await _context.Messages
                .Include(m => m.Listing)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == receiverId) ||
                            (m.SenderId == receiverId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    SentAt = m.SentAt.ToString("yyyy/MM/dd hh:mm tt"),
                    IsMine = m.SenderId == currentUserId, // هذه الميزة ستسهل جداً على مبرمج الجوال وضع رسائلك يمين ورسائل الطرف الآخر يسار
                    RelatedListingId = m.ListingId,
                    RelatedListingTitle = m.Listing != null ? m.Listing.Title : null
                })
                .ToListAsync();

            var receiver = await _context.Users.FindAsync(receiverId);

            return Ok(new
            {
                success = true,
                contactName = receiver?.FullName,
                data = chatHistory
            });
        }

        // 3. إرسال رسالة جديدة (POST: api/MessagesApi/Send)
        [HttpPost("Send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { success = false, message = "لا يمكن إرسال رسالة فارغة" });
            }

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            int currentUserId = int.Parse(userIdClaim);

            // التأكد من أن المستقبل موجود
            var receiverExists = await _context.Users.AnyAsync(u => u.Id == request.ReceiverId);
            if (!receiverExists)
            {
                return NotFound(new { success = false, message = "المستخدم غير موجود" });
            }

            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                SentAt = DateTime.Now,
                ListingId = request.ListingId
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "تم الإرسال بنجاح",
                sentMessage = new
                {
                    message.Id,
                    message.Content,
                    SentAt = message.SentAt.ToString("yyyy/MM/dd hh:mm tt"),
                    IsMine = true
                }
            });
        }
    }

    // كلاس بسيط لاستقبال البيانات من الجوال بصيغة JSON
    public class SendMessageRequest
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; }
        public int? ListingId { get; set; }
    }
}