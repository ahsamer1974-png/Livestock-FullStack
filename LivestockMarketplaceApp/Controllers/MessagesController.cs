using LivestockMarketplaceApp.Data;
using LivestockMarketplaceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LivestockMarketplaceApp.Controllers
{
    [Authorize] // حماية: لا يمكن فتح الرسائل إلا للمسجلين
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. عرض صفحة الرسائل (قائمة المحادثات + الدردشة المفتوحة)
        public async Task<IActionResult> Index(int? receiverId, int? listingId = null)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int currentUserId = int.Parse(userIdClaim);

            // أ. جلب قائمة الأشخاص الذين تواصلت معهم (سواء أرسلت لهم أو استقبلت منهم)
            var sentTo = await _context.Messages
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == currentUserId)
                .Select(m => m.Receiver)
                .ToListAsync();

            var receivedFrom = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == currentUserId)
                .Select(m => m.Sender)
                .ToListAsync();

            // دمج القائمتين وإزالة التكرار لعمل "قائمة جهات الاتصال"
            var contacts = sentTo.Concat(receivedFrom)
                .Where(u => u != null)
                .DistinctBy(u => u.Id)
                .ToList();

            // إرسال البيانات إلى صفحة العرض (View)
            ViewBag.Contacts = contacts;
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.ActiveReceiverId = receiverId;
            ViewBag.ActiveListingId = listingId; // حفظ رقم الإعلان إن وُجد

            // ب. إذا كان هناك شخص محدد تتحدث معه حالياً، نجلب تاريخ المحادثة بينكم
            List<Message> chatHistory = new List<Message>();
            if (receiverId.HasValue)
            {
                chatHistory = await _context.Messages
                    .Include(m => m.Listing) // جلب تفاصيل الإعلان المرتبط بالرسالة
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == receiverId) ||
                                (m.SenderId == receiverId && m.ReceiverId == currentUserId))
                    .OrderBy(m => m.SentAt) // استخدام SentAt بناءً على الموديل الخاص بك
                    .ToListAsync();

                // إرسال اسم الشخص الذي نراسله لعرضه في رأس المحادثة
                var receiver = await _context.Users.FindAsync(receiverId);
                ViewBag.ReceiverName = receiver?.FullName;
            }

            return View(chatHistory);
        }

        // 2. إرسال رسالة جديدة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int receiverId, string content, int? listingId)
        {
            // إذا كانت الرسالة فارغة، أعده لنفس المحادثة دون حفظ
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Index", new { receiverId = receiverId });
            }

            var userIdClaim = User.FindFirst("UserId")?.Value;
            int currentUserId = int.Parse(userIdClaim);

            // تجهيز الرسالة للحفظ
            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = receiverId,
                Content = content,
                SentAt = DateTime.Now, // استخدام SentAt
                ListingId = listingId // ربط الرسالة بالإعلان
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // العودة لنفس المحادثة لتظهر الرسالة الجديدة فوراً
            return RedirectToAction("Index", new { receiverId = receiverId });
        }
    }
}